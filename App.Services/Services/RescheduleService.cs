using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.BookingDTOs;
using App.DTOs.NotificationDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Notifications;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Infras;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace App.Services.Services
{
    public class RescheduleService : IRescheduleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly INotificationService _notificationService;

        public RescheduleService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
            _notificationService = notificationService;
        }

        #region Private Helpers
        private string GetAuthenticatedUserId()
        {
            var userId = _currentUserProvider.GetCurrentUserId();
            if (userId is null)
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "User is not authenticated.");
            return userId;
        }

        private async Task<BookedSlot> GetAndValidateBookedSlotAsync(string bookedSlotId, string tutorId)
        {
            var bookedSlot = await _unitOfWork.GetRepository<BookedSlot>()
                .ExistEntities()
                .Include(bs => bs.Booking)
                .ThenInclude(b => b!.Learner)
                .ThenInclude(l => l!.User)
                .FirstOrDefaultAsync(bs => bs.Id == bookedSlotId);

            if (bookedSlot == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Booked slot not found.");

            if (bookedSlot.Booking?.TutorId != tutorId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ErrorCode.Forbidden,
                    "You are not authorized to reschedule this slot.");

            if (bookedSlot.Status != SlotStatus.Pending)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Only pending slots can be rescheduled.");

            if (!RescheduleRequest.CanRequestReschedule(bookedSlot.BookedDate))
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Slots can only be rescheduled at least 24 hours before their start time.");

            return bookedSlot;
        }

        private void DeleteOfferedSlotsForRequest(RescheduleRequest request)
        {
            var offeredSlotRepo = _unitOfWork.GetRepository<OfferedSlot>();
            foreach (var slot in request.OfferedSlots.ToList())
            {
                offeredSlotRepo.Delete(slot);
            }
        }
        #endregion

        public async Task<RescheduleRequestResponse> CreateRescheduleRequestAsync(CreateRescheduleRequest request)
        {
            var userId = GetAuthenticatedUserId();
            
            // Only tutors can request rescheduling
            var tutor = await _unitOfWork.GetRepository<Tutor>()
                .ExistEntities()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ErrorCode.Forbidden,
                    "Only tutors can request rescheduling.");

            // Validate the booked slot
            var bookedSlot = await GetAndValidateBookedSlotAsync(request.BookedSlotId, userId);

            // Check if there's already a pending reschedule request for this slot
            var existingRequest = await _unitOfWork.GetRepository<RescheduleRequest>()
                .ExistEntities()
                .FirstOrDefaultAsync(r => r.BookedSlotId == request.BookedSlotId && r.Status == RescheduleRequestStatus.Pending);

            if (existingRequest != null)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "There is already a pending reschedule request for this slot.");

            // Create the reschedule request with the offered slot
            var rescheduleRequest = RescheduleRequest.Create(
                request.BookedSlotId,
                userId,
                RescheduleInitiator.Tutor,
                request.Reason,
                request.NewSlotDateTime,
                request.NewSlotIndex
            );

            // Save the request
            var rescheduleRepo = _unitOfWork.GetRepository<RescheduleRequest>();
            rescheduleRepo.Insert(rescheduleRequest);
            await _unitOfWork.SaveAsync();

            // Schedule expiration job
            HangfireConfig.ScheduleRescheduleExpirationJob(rescheduleRequest.Id, rescheduleRequest.ExpiresAt);

            // Send notification to learner
            var learnerId = bookedSlot.Booking!.LearnerId!;
            await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
            {
                Content = new()
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = "PUSH_ON_RESCHEDULE_REQUEST",
                    Content = "PUSH_ON_RESCHEDULE_REQUEST_BODY",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        SenderId = userId,
                        RescheduleRequestId = rescheduleRequest.Id,
                        BookedSlotId = bookedSlot.Id,
                        BookingId = bookedSlot.BookingId
                    })
                },
                ReceiverUserIds = [learnerId]
            });

            // Return the response
            return await GetRescheduleRequestByIdAsync(rescheduleRequest.Id);
        }

        public async Task<BasePaginatedList<RescheduleRequestResponse>> GetRescheduleRequestsAsync(
            int pageIndex = 0, 
            int pageSize = 10, 
            RescheduleRequestStatus? status = null)
        {
            var userId = GetAuthenticatedUserId();

            var query = _unitOfWork.GetRepository<RescheduleRequest>()
                .ExistEntities()
                .Include(r => r.BookedSlot)
                .ThenInclude(bs => bs!.Booking)
                .Include(r => r.OfferedSlots)
                .Where(r => r.RequestedByUserId == userId || 
                    (r.BookedSlot != null && r.BookedSlot.Booking != null && 
                    (r.BookedSlot.Booking.TutorId == userId || r.BookedSlot.Booking.LearnerId == userId)));

            if (status != null)
                query = query.Where(r => r.Status == status.Value);

            query = query.OrderByDescending(r => r.CreatedAt);

            var totalItems = await query.CountAsync();
            var requests = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var responses = requests.Select(RescheduleRequestResponse.FromEntity).ToList();
            return new BasePaginatedList<RescheduleRequestResponse>(
                items: responses,
                totalItems: totalItems,
                pageIndex: pageIndex,
                pageSize: pageSize
            );
        }

        public async Task<RescheduleRequestResponse> GetRescheduleRequestByIdAsync(string requestId)
        {
            var userId = GetAuthenticatedUserId();

            var request = await _unitOfWork.GetRepository<RescheduleRequest>()
                .ExistEntities()
                .Where(r => r.Id == requestId)
                .Select(RescheduleRequestResponse.Projection)
                .FirstOrDefaultAsync();

            if (request == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Reschedule request not found.");

            // Check authorization: we have the Tutor and Learner in the DTO
            if (request.Tutor == null || request.Learner == null)
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    ErrorCode.ServerError,
                    "Data inconsistency: Tutor or Learner not found in reschedule request.");

            if (request.Tutor.Id != userId && request.Learner.Id != userId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ErrorCode.Forbidden,
                    "You are not authorized to view this reschedule request.");

            return request;
        }

        public async Task<RescheduleRequestResponse> AcceptRescheduleRequestAsync(string requestId)
        {
            var userId = GetAuthenticatedUserId();

            return await _unitOfWork.ExecuteInTransactionAsync(async () => {
                // Get the reschedule request
                var request = await _unitOfWork.GetRepository<RescheduleRequest>()
                    .ExistEntities()
                    .Include(r => r.BookedSlot)
                    .ThenInclude(bs => bs!.Booking)
                    .ThenInclude(b => b!.Tutor)
                    .ThenInclude(t => t!.User)
                    .Include(r => r.OfferedSlots)
                    .FirstOrDefaultAsync(r => r.Id == requestId);

                if (request == null)
                    throw new ErrorException(
                        StatusCodes.Status404NotFound,
                        ErrorCode.NotFound,
                        "Reschedule request not found.");

                // Check if the user is the learner
                var booking = request.BookedSlot?.Booking;
                if (booking == null || booking.LearnerId != userId)
                    throw new ErrorException(
                        StatusCodes.Status403Forbidden,
                        ErrorCode.Forbidden,
                        "Only the learner can accept reschedule requests.");

                // Check if the request is still pending
                if (request.Status != RescheduleRequestStatus.Pending)
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        ErrorCode.BadRequest,
                        "Only pending reschedule requests can be accepted.");

                // Get the offered slot (there should be only one)
                var offeredSlot = request.OfferedSlots.FirstOrDefault();
                if (offeredSlot == null)
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        ErrorCode.BadRequest,
                        "No offered slot found in the reschedule request.");

                var oldHeldFundId = request.BookedSlot!.HeldFundId;

                request.BookedSlot!.HeldFundId = null;
                _unitOfWork.GetRepository<BookedSlot>().UpdateFields(request.BookedSlot, [x => x.HeldFundId!]);

                var newSlot = new BookedSlot
                {
                    BookingId = booking.Id,
                    BookedDate = offeredSlot.SlotDateTime,
                    SlotIndex = offeredSlot.SlotIndex,
                    Status = SlotStatus.Pending,
                    HeldFundId = oldHeldFundId,  
                    CreatedBy = userId,
                    LastUpdatedBy = userId,
                };
                
                _unitOfWork.GetRepository<BookedSlot>().Insert(newSlot);

                // Cancel the old slot
                var oldSlotUpdateFields = request.BookedSlot!.MarkAsCancelled(userId);
                _unitOfWork.GetRepository<BookedSlot>().UpdateFields(request.BookedSlot!, oldSlotUpdateFields);
                
                // Update the request status
                var updateFields = request.Accept(offeredSlot.Id, userId);
                _unitOfWork.GetRepository<RescheduleRequest>().UpdateFields(request, updateFields);
                
                await _unitOfWork.SaveAsync();
                
                // Send notification to tutor
                await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
                {
                    Content = new()
                    {
                        NotificationPriority = ENotificationPriority.Normal,
                        Title = "PUSH_ON_RESCHEDULE_ACCEPTED",
                        Content = "PUSH_ON_RESCHEDULE_ACCEPTED_BODY",
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            RescheduleRequestId = request.Id,
                            BookedSlotId = newSlot.Id,
                            BookingId = booking.Id
                        })
                    },
                    ReceiverUserIds = [booking.TutorId]
                });
                
                return RescheduleRequestResponse.FromEntity(request);
            });
        }

        public async Task<RescheduleRequestResponse> RejectRescheduleRequestAsync(string requestId, string? note)
        {
            var userId = GetAuthenticatedUserId();

            // Get the reschedule request
            var request = await _unitOfWork.GetRepository<RescheduleRequest>()
                .ExistEntities()
                .Include(r => r.BookedSlot)
                .ThenInclude(bs => bs!.Booking)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Reschedule request not found.");

            // Check if the user is the learner
            var booking = request.BookedSlot?.Booking;
            if (booking == null || booking.LearnerId != userId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ErrorCode.Forbidden,
                    "Only the learner can reject reschedule requests.");

            // Update the request status
            var updateFields = request.Reject(note ?? "No reason provided", userId);

            // Xóa offered slots trước khi cập nhật trạng thái
            DeleteOfferedSlotsForRequest(request);
            
            _unitOfWork.GetRepository<RescheduleRequest>().UpdateFields(request, updateFields);
            await _unitOfWork.SaveAsync();

            // Send notification to tutor
            await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
            {
                Content = new()
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = "PUSH_ON_RESCHEDULE_REJECTED",
                    Content = "PUSH_ON_RESCHEDULE_REJECTED_BODY",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        RescheduleRequestId = request.Id,
                        BookedSlotId = request.BookedSlotId,
                        BookingId = booking.Id,
                        RejectReason = note
                    })
                },
                ReceiverUserIds = [booking.TutorId]
            });

            return await GetRescheduleRequestByIdAsync(requestId);
        }

        public async Task<RescheduleRequestResponse> CancelRescheduleRequestAsync(string requestId)
        {
            var userId = GetAuthenticatedUserId();

            // Get the reschedule request
            var request = await _unitOfWork.GetRepository<RescheduleRequest>()
                .ExistEntities()
                .Include(r => r.BookedSlot)
                .ThenInclude(bs => bs!.Booking)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Reschedule request not found.");

            // Check if the user is the requester
            if (request.RequestedByUserId != userId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ErrorCode.Forbidden,
                    "Only the requester can cancel the reschedule request.");

            // Update the request status
            var updateFields = request.Cancel(userId);

            // Xóa offered slots trước khi cập nhật trạng thái
            DeleteOfferedSlotsForRequest(request);
            
            _unitOfWork.GetRepository<RescheduleRequest>().UpdateFields(request, updateFields);
            await _unitOfWork.SaveAsync();

            // Send notification to the other party (learner)
            var learnerId = request.BookedSlot?.Booking?.LearnerId;
            if (learnerId != null)
            {
                await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
                {
                    Content = new()
                    {
                        NotificationPriority = ENotificationPriority.Normal,
                        Title = "PUSH_ON_RESCHEDULE_CANCELLED",
                        Content = "PUSH_ON_RESCHEDULE_CANCELLED_BODY",
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            RescheduleRequestId = request.Id,
                            BookedSlotId = request.BookedSlotId,
                            BookingId = request.BookedSlot?.BookingId
                        })
                    },
                    ReceiverUserIds = [learnerId]
                });
            }

            return await GetRescheduleRequestByIdAsync(requestId);
        }

        public async Task DeleteRescheduleRequestAsync(string requestId)
        {
            var userId = GetAuthenticatedUserId();

            // Lấy thông tin yêu cầu
            var request = await _unitOfWork.GetRepository<RescheduleRequest>()
                .ExistEntities()
                .Include(r => r.OfferedSlots)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy yêu cầu thay đổi lịch.");

            // Kiểm tra quyền: chỉ người tạo yêu cầu mới được xóa
            if (request.RequestedByUserId != userId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ErrorCode.Forbidden,
                    "Bạn không có quyền xóa yêu cầu này.");

            // Chỉ cho phép xóa yêu cầu đang ở trạng thái Pending
            if (request.Status != RescheduleRequestStatus.Pending)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Chỉ có thể xóa yêu cầu đang chờ phản hồi.");

            // Xóa offered slots trước khi xóa request
            DeleteOfferedSlotsForRequest(request);

            _unitOfWork.GetRepository<RescheduleRequest>().Delete(request);
            await _unitOfWork.SaveAsync();
        }
        public async Task<Dictionary<string, object>> GetRescheduleMetadataAsync()
        {
            var enumMetadata = EnumHelper.GetEnumMetadata(
                typeof(RescheduleRequestStatus),
                typeof(RescheduleInitiator)
            );

            return await Task.FromResult(enumMetadata.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)kvp.Value
            ));
        }
    }
}
