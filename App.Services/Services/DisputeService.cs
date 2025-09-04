using App.Core.Base;
using App.Core.Provider;
using App.DTOs.BookingDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Repositories.Models.Notifications;
using Microsoft.EntityFrameworkCore;
using App.Core.Constants;
using App.DTOs.NotificationDTOs;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Hosting;

namespace App.Services.Services
{
    public class DisputeService : IDisputeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly IWalletService _walletService;
        private readonly INotificationService _notificationService;
        private readonly ITutorBookingService _tutorBookingService;

        public DisputeService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider,
            IWalletService walletService,
            INotificationService notificationService,
            ITutorBookingService tutorBookingService)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
            _walletService = walletService;
            _notificationService = notificationService;
            _tutorBookingService = tutorBookingService;
        }

        #region Helper Methods
        private string GetAuthenticatedUserId()
        {
            var userId = _currentUserProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized, 
                    ErrorCode.Unauthorized, 
                    "Người dùng chưa xác thực");

            return userId;
        }

        private async Task<string> GetAuthenticatedLearnerIdAsync()
        {
            var userId = GetAuthenticatedUserId();
            var learner = await _unitOfWork.GetRepository<Learner>().FindAsync(l => l.UserId == userId);
            
            if (learner == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Không tìm thấy thông tin học viên");

            return learner.UserId;
        }

        private async Task<string> GetAuthenticatedTutorIdAsync()
        {
            var userId = GetAuthenticatedUserId();
            var tutor = await _unitOfWork.GetRepository<Tutor>().FindAsync(t => t.UserId == userId);
            
            if (tutor == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Không tìm thấy thông tin gia sư");

            return tutor.UserId;
        }

        private async Task<BookedSlot> GetAndValidateBookedSlotAsync(string bookedSlotId, string learnerId)
        {
            var bookedSlot = await _unitOfWork.GetRepository<BookedSlot>()
                .GetQueryable()
                .Include(bs => bs.Booking)
                    .ThenInclude(b => b!.Tutor)
                .Include(bs => bs.Booking)
                    .ThenInclude(b => b!.Learner)
                .FirstOrDefaultAsync(bs => bs.Id == bookedSlotId && bs.Booking!.LearnerId == learnerId);
                
            if (bookedSlot == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Không tìm thấy thông tin slot đã đặt");

            if (bookedSlot.Status == SlotStatus.Cancelled || bookedSlot.Status == SlotStatus.CancelledDisputed)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Slot này đã bị hủy");

            if (bookedSlot.Status == SlotStatus.Completed)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Slot này đã hoàn thành. Không thể khiếu nại");

            if (bookedSlot.Status != SlotStatus.AwaitingPayout)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Slot này chưa đến giai đoạn được khiếu nại.");

            if (DateTime.UtcNow > bookedSlot.GetSlotStartTime.AddDays(1))
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Bạn chỉ có thể khiếu nại trong vòng 24 giờ sau khi slot kết thúc");

            if (!string.IsNullOrEmpty(bookedSlot.DisputeId))
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Slot này đang trong quá trình tranh chấp");
                
            return bookedSlot;
        }

        private async Task<BookingDispute> GetDisputeAsync(string disputeId)
        {
            var dispute = await _unitOfWork.GetRepository<BookingDispute>()
                .GetQueryable()
                .Include(d => d.BookedSlot)
                    .ThenInclude(bs => bs!.Booking)
                .Include(d => d.Learner)
                .Include(d => d.Tutor)
                .FirstOrDefaultAsync(d => d.Id == disputeId);
                
            if (dispute == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Không tìm thấy thông tin khiếu nại");
                
            return dispute;
        }

        private void DisputeEligibleForEdit(BookingDispute dispute, Role role)
        {
            if (dispute.ReconciliationEndTime < DateTime.UtcNow)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Thời gian giải quyết đã kết thúc. Không thể chỉnh sửa khiếu nại");
            switch (role)
            {
                case Role.Learner:
                    if (dispute.Status != DisputeStatus.PendingReconciliation && dispute.Status != DisputeStatus.AwaitingStaffReview)
                        throw new ErrorException(
                            StatusCodes.Status400BadRequest,
                            ErrorCode.BadRequest,
                            "Chỉ có thể chỉnh sửa khiếu nại trong giai đoạn hòa giải");
                    break;
                case Role.Tutor:
                    if (dispute.Status != DisputeStatus.PendingReconciliation)
                        throw new ErrorException(
                            StatusCodes.Status400BadRequest,
                            ErrorCode.BadRequest,
                            "Chỉ có thể chỉnh sửa khiếu nại trong giai đoạn hòa giải");
                    break;
                case Role.Staff:
                case Role.Manager:
                case Role.Admin:
                    if (dispute.Status != DisputeStatus.AwaitingStaffReview)
                        throw new ErrorException(
                            StatusCodes.Status400BadRequest,
                            ErrorCode.BadRequest,
                            "Chỉ có thể chỉnh sửa khiếu nại đang chờ Staff Review");
                    break;
            }
        }


        private async Task ProcessDisputeResolution(BookingDispute dispute, DisputeResolution resolution)
        {
            var bookedSlotRepo = _unitOfWork.GetRepository<BookedSlot>();
            var bookedSlot = dispute.BookedSlot;
            
            if (bookedSlot == null)
            {
                bookedSlot = await bookedSlotRepo.GetByIdAsync(dispute.BookedSlotId);
                if (bookedSlot == null)
                    return;
            }
                
            // Process funds based on resolution
            if (!string.IsNullOrEmpty(bookedSlot.HeldFundId))
            {
                var heldFund = await _unitOfWork.GetRepository<HeldFund>().GetByIdAsync(bookedSlot.HeldFundId);
                if (heldFund != null)
                {
                    Expression<Func<BookedSlot, object>>[] updateProperties;
                    switch (resolution)
                    {
                        case DisputeResolution.LearnerWithdrew:
                        case DisputeResolution.StaffTutorWin:
                            updateProperties = bookedSlot.UpdateStatus(SlotStatus.AwaitingPayout, GetAuthenticatedUserId());
                            bookedSlotRepo.UpdateFields(bookedSlot, updateProperties);
                            break;
                            
                        case DisputeResolution.TutorNoResponse:
                        case DisputeResolution.StaffLearnerWin:
                        case DisputeResolution.TutorFullRefund:
                            updateProperties = bookedSlot.UpdateStatus(SlotStatus.CancelledDisputed, GetAuthenticatedUserId());
                            await _walletService.RefundHeldFundToLearnerAsync(heldFund.Id);
                            break;
                            
                        case DisputeResolution.StaffDraw:
                        case DisputeResolution.TutorPartialRefund:
                            updateProperties = bookedSlot.UpdateStatus(SlotStatus.CancelledDisputed, GetAuthenticatedUserId());
                            // 50% to tutor, 50% to learner
                            await _walletService.PartialRefundForDisputeAsync(
                                heldFund.Id,
                                0.5m, // 50% to tutor
                                bookedSlot.BookingId);
                            break;
                    }
                }
            }
            // Send notifications
            await SendDisputeResolvedNotificationsAsync(dispute, resolution);
        }

        private async Task SendDisputeCreatedNotificationsAsync(BookingDispute dispute)
        {
            // To Learner
            await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
            {
                ReceiverUserIds = new List<string> { dispute.LearnerId },
                Content = new NotificationRequest
                {
                    NotificationPriority =  ENotificationPriority.Normal,
                    Title = $"Khiếu nại {dispute.CaseNumber} đã được tạo",
                    Content = "Bạn và gia sư có 24 giờ để trao đổi và giải quyết khiếu nại.",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Type = "DisputeCreated",
                        ReferenceId = dispute.Id,
                        ReferenceType = "BookingDispute"
                    })
                }
            });
            
            // To Tutor
            await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
            {
                ReceiverUserIds = new List<string> { dispute.TutorId },
                Content = new NotificationRequest
                {
                    NotificationPriority =  ENotificationPriority.Normal,
                    Title = $"Bạn có khiếu nại mới: {dispute.CaseNumber}",
                    Content = "Bạn cần phản hồi trong vòng 24 giờ. Nếu không phản hồi, hệ thống sẽ xử lý theo hướng có lợi cho học viên.",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Type = "DisputeReceived",
                        ReferenceId = dispute.Id,
                        ReferenceType = "BookingDispute"
                    })
                }
            });
        }

        private async Task SendDisputeResolvedNotificationsAsync(BookingDispute dispute, DisputeResolution resolution)
        {
            string tutorMessage, learnerMessage, staffMessage = "";
            
            switch (resolution)
            {
                case DisputeResolution.LearnerWithdrew:
                    tutorMessage = $"Học viên đã rút lại khiếu nại {dispute.CaseNumber}. Slot tiếp tục như bình thường.";
                    learnerMessage = $"Bạn đã rút lại khiếu nại {dispute.CaseNumber}. Slot tiếp tục như bình thường.";
                    break;
                    
                case DisputeResolution.TutorNoResponse:
                    tutorMessage = $"Bạn không phản hồi khiếu nại {dispute.CaseNumber} đúng hạn. Hệ thống đã hủy slot và hoàn tiền cho học viên.";
                    learnerMessage = $"Gia sư không phản hồi khiếu nại {dispute.CaseNumber}. Hệ thống đã hủy slot và hoàn tiền cho bạn.";
                    break;
                    
                case DisputeResolution.StaffLearnerWin:
                    tutorMessage = $"Nhân viên hệ thống đã xử lý khiếu nại {dispute.CaseNumber} và quyết định hoàn tiền cho học viên.";
                    learnerMessage = $"Nhân viên hệ thống đã xử lý khiếu nại {dispute.CaseNumber} theo hướng có lợi cho bạn. Tiền sẽ được hoàn lại vào ví của bạn.";
                    staffMessage = $"Đã xử lý khiếu nại {dispute.CaseNumber} theo hướng có lợi cho học viên.";
                    break;
                    
                case DisputeResolution.StaffTutorWin:
                    tutorMessage = $"Nhân viên hệ thống đã xử lý khiếu nại {dispute.CaseNumber} theo hướng có lợi cho bạn. Bạn sẽ nhận được 5% tiền công, 95% sẽ được hoàn cho học viên.";
                    learnerMessage = $"Nhân viên hệ thống đã xử lý khiếu nại {dispute.CaseNumber} theo hướng có lợi cho gia sư. Bạn sẽ được hoàn lại 95% số tiền.";
                    staffMessage = $"Đã xử lý khiếu nại {dispute.CaseNumber} theo hướng có lợi cho gia sư.";
                    break;
                    
                case DisputeResolution.StaffDraw:
                    tutorMessage = $"Nhân viên hệ thống đã xử lý khiếu nại {dispute.CaseNumber} và xác định đây là hòa. Học viên sẽ được hoàn tiền.";
                    learnerMessage = $"Nhân viên hệ thống đã xử lý khiếu nại {dispute.CaseNumber} và xác định đây là hòa. Bạn sẽ được hoàn lại toàn bộ tiền.";
                    staffMessage = $"Đã xử lý khiếu nại {dispute.CaseNumber} với kết quả hòa.";
                    break;
                case DisputeResolution.TutorPartialRefund:
                    tutorMessage = $"Bạn đã đồng ý hoàn lại 50% số tiền cho học viên trong khiếu nại {dispute.CaseNumber}.";
                    learnerMessage = $"Gia sư đã đồng ý hoàn lại 50% số tiền cho bạn trong khiếu nại {dispute.CaseNumber}.";
                    break;
                case DisputeResolution.TutorFullRefund:
                    tutorMessage = $"Bạn đã đồng ý hoàn lại 100% số tiền cho học viên trong khiếu nại {dispute.CaseNumber}.";
                    learnerMessage = $"Gia sư đã đồng ý hoàn lại 100% số tiền cho bạn trong khiếu nại {dispute.CaseNumber}.";
                    break;
                default:
                    tutorMessage = $"Khiếu nại {dispute.CaseNumber} đã được giải quyết.";
                    learnerMessage = $"Khiếu nại {dispute.CaseNumber} đã được giải quyết.";
                    break;
            }
            
            // To Tutor
            await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
            {
                ReceiverUserIds = new List<string> { dispute.TutorId },
                Content = new NotificationRequest
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = $"Kết quả khiếu nại {dispute.CaseNumber}",
                    Content = tutorMessage,
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Type = "DisputeResolved",
                        ReferenceId = dispute.Id,
                        ReferenceType = "BookingDispute"
                    })
                }
            });
            
            // To Learner
            await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
            {
                ReceiverUserIds = new List<string> { dispute.LearnerId },
                Content = new NotificationRequest
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = $"Kết quả khiếu nại {dispute.CaseNumber}",
                    Content = learnerMessage,
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Type = "DisputeResolved",
                        ReferenceId = dispute.Id,
                        ReferenceType = "BookingDispute"
                    })
                }
            });
            
            // To Staff (if applicable)
            if (!string.IsNullOrEmpty(dispute.StaffId) && !string.IsNullOrEmpty(staffMessage))
            {
                await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
                {
                    ReceiverUserIds = new List<string> { dispute.StaffId },
                    Content = new NotificationRequest
                    {
                        NotificationPriority = ENotificationPriority.Normal,
                        Title = $"Kết quả xử lý khiếu nại {dispute.CaseNumber}",
                        Content = staffMessage,
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            Type = "DisputeResolved",
                            ReferenceId = dispute.Id,
                            ReferenceType = "BookingDispute"
                        })
                    }
                });
            }
        }

        private async Task<decimal> CalculateDisputedAmount(BookedSlot slot)
        {
            // Get HeldFund amount if available
            if (!string.IsNullOrEmpty(slot.HeldFundId))
            {
                var heldFund = await _unitOfWork.GetRepository<HeldFund>().GetByIdAsync(slot.HeldFundId);
                if (heldFund != null)
                {
                    return heldFund.Amount;
                }
            }
            
            // Otherwise try to get from booking's lesson
            if (slot.Booking?.LessonSnapshot != null)
            {
                // Use the snapshot price
                var lessonSnapshot = await _unitOfWork.GetRepository<LessonSnapshot>().GetByIdAsync(slot.Booking.LessonSnapshotId!);
                if (lessonSnapshot != null)
                {
                    return lessonSnapshot.Price;
                }
            }
            
            return 0;
        }
        #endregion

        #region Learner Operations
        public async Task<BookingDisputeResponse> CreateDisputeAsync(CreateDisputeRequest request)
        {
            var learnerId = await GetAuthenticatedLearnerIdAsync();
            var bookedSlot = await GetAndValidateBookedSlotAsync(request.BookedSlotId, learnerId);
            
            // Create evidence URLs JSON if provided
            string? evidenceUrlsJson = null;
            if (request.EvidenceUrls != null && request.EvidenceUrls.Any())
                evidenceUrlsJson = JsonSerializer.Serialize(request.EvidenceUrls);
            
            // Create dispute
            var dispute = BookingDispute.CreateDispute(
                bookedSlot.Id,
                learnerId,
                bookedSlot.Booking!.TutorId,
                request.Reason,
                evidenceUrlsJson);

            var disputeRepo = _unitOfWork.GetRepository<BookingDispute>();
            var bookedSlotRepo = _unitOfWork.GetRepository<BookedSlot>();
            var bookingRepo = _unitOfWork.GetRepository<Booking>();
                
            // Add to database
            disputeRepo.Insert(dispute);
            
            // Update slot with dispute reference
            var slotUpdateProperties = bookedSlot.MarkAsCancelledDisputed(dispute.Id, learnerId);
            bookedSlotRepo.UpdateFields(bookedSlot, slotUpdateProperties.ToArray());
            
            await _unitOfWork.SaveAsync();
            
            // Send notifications
            await SendDisputeCreatedNotificationsAsync(dispute);
            
            return await GetDisputeResponseAsync(dispute.Id);
        }

        public async Task<BookingDisputeResponse> WithdrawDisputeAsync(WithdrawDisputeRequest request)
        {
            var learnerId = await GetAuthenticatedLearnerIdAsync();
            var dispute = await GetDisputeAsync(request.DisputeId);

            // Verify learner owns this dispute
            if (dispute.LearnerId != learnerId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden, 
                    ErrorCode.Forbidden, 
                    "Bạn không có quyền rút lại khiếu nại này");
                
            DisputeEligibleForEdit(dispute, Role.Learner);

            // Update dispute
            var updateProperties = dispute.WithdrawDispute();
            _unitOfWork.GetRepository<BookingDispute>().UpdateFields(dispute, updateProperties.ToArray());
            
            // Process resolution
            await ProcessDisputeResolution(dispute, DisputeResolution.LearnerWithdrew);
            
            await _unitOfWork.SaveAsync();
            
            return await GetDisputeResponseAsync(dispute.Id);
        }

        public async Task<List<BookingDisputeResponse>> GetLearnerDisputesAsync(bool? onlyActive = null)
        {
            var learnerId = await GetAuthenticatedLearnerIdAsync();
            
            var query = _unitOfWork.GetRepository<BookingDispute>()
                .GetQueryable()
                .Where(d => d.LearnerId == learnerId);
                
            if (onlyActive == true)
            {
                query = query.Where(d => d.Status == DisputeStatus.PendingReconciliation || 
                                        d.Status == DisputeStatus.AwaitingStaffReview);
            }
            
            var disputes = await query.ToListAsync();
            var responses = new List<BookingDisputeResponse>();
            
            foreach (var dispute in disputes)
            {
                responses.Add(await GetDisputeResponseAsync(dispute.Id));
            }
            
            return responses;
        }

        public async Task<DisputeDetailResponse> GetDisputeDetailForLearnerAsync(string disputeId)
        {
            var learnerId = await GetAuthenticatedLearnerIdAsync();
            var dispute = await GetDisputeAsync(disputeId);
            
            // Verify learner owns this dispute
            if (dispute.LearnerId != learnerId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden, 
                    ErrorCode.Forbidden, 
                    "Bạn không có quyền xem chi tiết khiếu nại này");
                
            return await GetDisputeDetailResponseAsync(dispute);
        }
        #endregion

        #region Tutor Operations

        public async Task<BasePaginatedList<BookingDisputeResponse>> GetFilteredDisputesAsync(StaffDisputeFilterRequest filter)
        {
            var userId = GetAuthenticatedUserId();
            EnsureHasManagerialAccess("Bạn không có quyền xem danh sách khiếu nại");
            
            var query = _unitOfWork.GetRepository<BookingDispute>()
                .GetQueryable()
                .Include(d => d.Learner).ThenInclude(l => l!.User)
                .Include(d => d.Tutor).ThenInclude(t => t!.User)
                .AsQueryable();
            
            // Lọc theo resolution nếu có
            if (filter.ResolutionFilter != null && filter.ResolutionFilter.Count > 0)
                query = query.Where(d => filter.ResolutionFilter.Contains(d.Resolution));
            
            // Tìm theo case number nếu có
            if (!string.IsNullOrWhiteSpace(filter.CaseNumber))
                query = query.Where(d => d.CaseNumber.Contains(filter.CaseNumber));
            
            // Sắp xếp theo ngày gần nhất
            query = query.OrderByDescending(d => d.CreatedAt)
                        .ThenByDescending(d => d.StaffReviewEndTime);
            
            // Đếm tổng số kết quả
            var totalCount = await query.CountAsync();
            
            // Lấy phân trang
            var pageSize = Math.Max(1, filter.PageSize);
            var pageIndex = Math.Max(0, filter.PageIndex);
            
            var disputes = await query
                .Skip(pageIndex * pageSize)
                .Take(pageSize)
                .ToListAsync();
            
            // Chuyển thành response
            var responses = disputes.Select(d => BookingDisputeResponse.Projection.Compile().Invoke(d)).ToList();
            
            return new BasePaginatedList<BookingDisputeResponse>(
                responses, 
                totalCount, 
                pageIndex, 
                pageSize
            );
        }

        public async Task<BookingDisputeResponse> RespondToDisputeAsync(RespondToDisputeRequest request)
        {
            var tutorId = await GetAuthenticatedTutorIdAsync();
            var dispute = await GetDisputeAsync(request.DisputeId);
            
            // Verify tutor owns this dispute
            //if (dispute.TutorId != tutorId)
            //    throw new ErrorException(
            //        StatusCodes.Status403Forbidden, 
            //        ErrorCode.Forbidden, 
            //        "Bạn không có quyền phản hồi khiếu nại này");

            DisputeEligibleForEdit(dispute, Role.Tutor);
            var disputeRepo = _unitOfWork.GetRepository<BookingDispute>();

            // Update dispute with response
            Expression<Func<BookingDispute, object>>[] updateProperties;
            switch (request.Resolution)
            {
                case DisputeResolution.TutorPartialRefund:
                    updateProperties = dispute.AddTutorResponse(request.Response, request.Resolution);
                    disputeRepo.UpdateFields(dispute, updateProperties.ToArray());
                    await ProcessDisputeResolution(dispute, DisputeResolution.TutorPartialRefund);
                    await _unitOfWork.SaveAsync();
                    break;
                case DisputeResolution.TutorFullRefund:
                    updateProperties = dispute.AddTutorResponse(request.Response, request.Resolution);
                    disputeRepo.UpdateFields(dispute, updateProperties.ToArray());
                    await ProcessDisputeResolution(dispute, DisputeResolution.TutorFullRefund);
                    await _unitOfWork.SaveAsync();
                    break;
                case DisputeResolution.None:
                    // Escalate to staff review
                    // Get booking and escalate
                    var bookedSlot = dispute.BookedSlot ?? await _unitOfWork.GetRepository<BookedSlot>().GetByIdAsync(dispute.BookedSlotId);
                    var staffId = await GetSystemStaffIdAsync();
                    updateProperties = dispute.EscalateToStaff(staffId);
                    disputeRepo.UpdateFields(dispute, updateProperties.ToArray());

                    // Update HeldFund status if exists
                    if (!string.IsNullOrEmpty(bookedSlot?.HeldFundId))
                    {
                        var heldFund = await _unitOfWork.GetRepository<HeldFund>().GetByIdAsync(bookedSlot.HeldFundId);
                        if (heldFund != null)
                        {
                            var updateFundProperties = heldFund.UpdateStatus(HeldFundStatus.Disputed);
                            _unitOfWork.GetRepository<HeldFund>().UpdateFields(heldFund, updateFundProperties.ToArray());
                        }
                    }
                    await _unitOfWork.SaveAsync();
                    // Notify staff and parties
                    await NotifyDisputeEscalatedAsync(dispute);
                    break;
            }   
            return await GetDisputeResponseAsync(dispute.Id);
        }

        private async Task<string> GetSystemStaffIdAsync()
        {
            // Get a system staff member to assign the dispute
            var staff = await _unitOfWork.GetRepository<Staff>()
                .GetQueryable()
                .FirstOrDefaultAsync();
                
            if (staff == null)
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError, 
                    ErrorCode.ServerError, 
                    "Không tìm thấy nhân viên xử lý");
                
            return staff.UserId;
        }

        private async Task NotifyDisputeEscalatedAsync(BookingDispute dispute)
        {
            // To Staff
            if (!string.IsNullOrEmpty(dispute.StaffId))
            {
                await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
                {
                    ReceiverUserIds = new List<string> { dispute.StaffId },
                    Content = new NotificationRequest
                    {
                        NotificationPriority = ENotificationPriority.Normal,
                        Title = $"Khiếu nại mới cần xử lý: {dispute.CaseNumber}",
                        Content = "Khiếu nại đã được leo thang và cần được xử lý trong vòng 48 giờ.",
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            Type = "DisputeEscalated",
                            ReferenceId = dispute.Id,
                            ReferenceType = "BookingDispute"
                        })
                    }
                });
            }
            // To Learner
            await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
            {
                ReceiverUserIds = new List<string> { dispute.LearnerId },
                Content = new NotificationRequest
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = $"Khiếu nại {dispute.CaseNumber} đã được chuyển lên hệ thống",
                    Content = "Gia sư đã phản hồi và khiếu nại của bạn đã được chuyển cho nhân viên hệ thống xử lý. Vui lòng chờ trong vòng 48 giờ.",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Type = "DisputeEscalated",
                        ReferenceId = dispute.Id,
                        ReferenceType = "BookingDispute"
                    })
                }
            });
            
            // To Tutor
            await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest
            {
                ReceiverUserIds = new List<string> { dispute.TutorId },
                Content = new NotificationRequest
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = $"Khiếu nại {dispute.CaseNumber} đã được chuyển lên hệ thống",
                    Content = "Phản hồi của bạn đã được ghi nhận và khiếu nại đã được chuyển cho nhân viên hệ thống xử lý. Vui lòng chờ kết quả trong vòng 48 giờ.",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Type = "DisputeEscalated",
                        ReferenceId = dispute.Id,
                        ReferenceType = "BookingDispute"
                    })
                }
            });
        }

        public async Task<List<BookingDisputeResponse>> GetTutorDisputesAsync(bool? onlyActive = null)
        {
            var tutorId = await GetAuthenticatedTutorIdAsync();
            
            var query = _unitOfWork.GetRepository<BookingDispute>()
                .GetQueryable()
                .Where(d => d.TutorId == tutorId);
                
            if (onlyActive == true)
            {
                query = query.Where(d => d.Status == DisputeStatus.PendingReconciliation || 
                                        d.Status == DisputeStatus.AwaitingStaffReview);
            }
            
            var disputes = await query.ToListAsync();
            var responses = new List<BookingDisputeResponse>();
            
            foreach (var dispute in disputes)
            {
                responses.Add(await GetDisputeResponseAsync(dispute.Id));
            }
            
            return responses;
        }

        public async Task<DisputeDetailResponse> GetDisputeDetailForTutorAsync(string disputeId)
        {
            var tutorId = await GetAuthenticatedTutorIdAsync();
            var dispute = await GetDisputeAsync(disputeId);
            
            // Verify tutor owns this dispute
            if (dispute.TutorId != tutorId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden, 
                    ErrorCode.Forbidden, 
                    "Bạn không có quyền xem chi tiết khiếu nại này");
                
            return await GetDisputeDetailResponseAsync(dispute);
        }
        #endregion

        #region Staff Operations
        private void EnsureHasManagerialAccess(string message = "Bạn không có quyền thực hiện thao tác này")
        {
            var isAdmin = _currentUserProvider.IsInRole(Role.Admin.ToStringRole());
            var isStaff = _currentUserProvider.IsInRole(Role.Staff.ToStringRole());
            
            if (!isAdmin && !isStaff)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ErrorCode.Forbidden,
                    message);
        }

        public async Task<BookingDisputeResponse> ResolveDisputeAsync(ResolveDisputeRequest request)
        {
            EnsureHasManagerialAccess("Bạn không có quyền xử lý khiếu nại");
            var dispute = await GetDisputeAsync(request.DisputeId);
            DisputeEligibleForEdit(dispute, Role.Staff);

            // Resolve dispute
            var updateProperties = dispute.ResolveByStaff(request.Resolution, request.Notes);
            _unitOfWork.GetRepository<BookingDispute>().UpdateFields(dispute, updateProperties.ToArray());
            
            // Process resolution
            await ProcessDisputeResolution(dispute, request.Resolution);
            
            await _unitOfWork.SaveAsync();
            
            return await GetDisputeResponseAsync(dispute.Id);
        }

        public async Task<List<BookingDisputeResponse>> GetDisputesForReviewAsync()
        {
            var userId = GetAuthenticatedUserId();
            EnsureHasManagerialAccess("Bạn không có quyền xem danh sách khiếu nại");
            
            var query = _unitOfWork.GetRepository<BookingDispute>()
                .GetQueryable()
                .Where(d => d.Status == DisputeStatus.AwaitingStaffReview);
            
            var disputes = await query.ToListAsync();
            var responses = new List<BookingDisputeResponse>();
            
            foreach (var dispute in disputes)
            {
                responses.Add(await GetDisputeResponseAsync(dispute.Id));
            }
            
            return responses;
        }

        public async Task<DisputeDetailResponse> GetDisputeDetailForStaffAsync(string disputeId)
        {
            var userId = GetAuthenticatedUserId();
            var dispute = await GetDisputeAsync(disputeId);
            
            EnsureHasManagerialAccess("Bạn không có quyền xem chi tiết khiếu nại này");
            
            return await GetDisputeDetailResponseAsync(dispute);
        }
        #endregion

        #region System Operations
        public async Task ProcessExpiredReconciliationsAsync()
        {
            // Find disputes where:
            // 1. Status is PendingReconciliation
            // 2. ReconciliationEndTime has passed
            // 3. No tutor response yet
            var expiredDisputes = await _unitOfWork.GetRepository<BookingDispute>()
                .GetQueryable()
                .Include(d => d.BookedSlot)
                    .ThenInclude(bs => bs!.Booking)
                .Where(d => d.Status == DisputeStatus.PendingReconciliation &&
                            d.ReconciliationEndTime < DateTime.UtcNow &&
                            string.IsNullOrEmpty(d.TutorResponse))
                .ToListAsync();
                
            foreach (var dispute in expiredDisputes)
            {
                try
                {
                    // Resolve as tutor no-response
                    var updateProperties = dispute.ResolveNoTutorResponse();
                    _unitOfWork.GetRepository<BookingDispute>().UpdateFields(dispute, updateProperties.ToArray());
                    
                    // Process resolution
                    await ProcessDisputeResolution(dispute, DisputeResolution.TutorNoResponse);
                    
                    await _unitOfWork.SaveAsync();
                }
                catch (Exception ex)
                {
                    // Log exception but continue processing
                    Console.WriteLine($"Error processing expired reconciliation for dispute {dispute.Id}: {ex.Message}");
                }
            }
        }

        public async Task ProcessExpiredStaffReviewsAsync()
        {
            // Find disputes where:
            // 1. Status is AwaitingStaffReview
            // 2. StaffReviewEndTime has passed
            var expiredDisputes = await _unitOfWork.GetRepository<BookingDispute>()
                .GetQueryable()
                .Include(d => d.BookedSlot)
                    .ThenInclude(bs => bs!.Booking)
                .Where(d => d.Status == DisputeStatus.AwaitingStaffReview &&
                            d.StaffReviewEndTime.HasValue &&
                            d.StaffReviewEndTime.Value < DateTime.UtcNow)
                .ToListAsync();
                
            foreach (var dispute in expiredDisputes)
            {
                try
                {
                    // Auto-resolve as draw when staff doesn't respond in time
                    var updateProperties = dispute.ResolveByStaff(
                        DisputeResolution.StaffDraw, 
                        "Tự động giải quyết là hòa do hết thời gian xử lý.");
                    _unitOfWork.GetRepository<BookingDispute>().UpdateFields(dispute, updateProperties.ToArray());
                    
                    // Process resolution
                    await ProcessDisputeResolution(dispute, DisputeResolution.StaffDraw);
                    
                    await _unitOfWork.SaveAsync();
                }
                catch (Exception ex)
                {
                    // Log exception but continue processing
                    Console.WriteLine($"Error processing expired staff review for dispute {dispute.Id}: {ex.Message}");
                }
            }
        }
        #endregion

        #region Shared Response Methods
        private async Task<BookingDisputeResponse> GetDisputeResponseAsync(string disputeId)
        {
            var dispute = await _unitOfWork.GetRepository<BookingDispute>()
                .GetQueryable()
                .Include(d => d.BookedSlot)
                    .ThenInclude(bs => bs!.Booking)
                .Include(d => d.Learner).ThenInclude(l => l!.User)
                .Include(d => d.Tutor).ThenInclude(t => t!.User)
                .FirstOrDefaultAsync(d => d.Id == disputeId);
                
            if (dispute == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Không tìm thấy thông tin khiếu nại");
                
            return BookingDisputeResponse.Projection.Compile().Invoke(dispute);
        }

        private async Task<DisputeDetailResponse> GetDisputeDetailResponseAsync(BookingDispute dispute)
        {
            var bookingDisputeResponse = await GetDisputeResponseAsync(dispute.Id);
            
            // Get the specific disputed slot
            var disputedSlot = dispute.BookedSlot ?? await _unitOfWork.GetRepository<BookedSlot>()
                .GetQueryable()
                .Include(bs => bs.HeldFund)
                .FirstOrDefaultAsync(bs => bs.Id == dispute.BookedSlotId);
                
            var slotResponses = new List<BookedSlotDTO>();
            if (disputedSlot != null)
            {
                slotResponses.Add(new BookedSlotDTO
                {
                    Id = disputedSlot.Id,
                    BookedDate = disputedSlot.BookedDate,
                    SlotIndex = disputedSlot.SlotIndex,
                    Status = disputedSlot.Status
                });
            }
            
            // Calculate disputed amount for this specific slot
            var disputedAmount = disputedSlot != null ? await CalculateDisputedAmount(disputedSlot) : 0;
            
            return new DisputeDetailResponse
            {
                Dispute = bookingDisputeResponse,
                AffectedSlots = slotResponses,
                DisputedAmount = disputedAmount
            };
        }
        #endregion
        public Task<Dictionary<string, object>> GetDisputeMetadataAsync()
        {
            var enumMetadata = EnumHelper.GetEnumMetadata(
                typeof(DisputeStatus),
                typeof(DisputeResolution),
                typeof(BookingStatus),
                typeof(SlotStatus)
            );
            
            var result = new Dictionary<string, object>();
            foreach (var kv in enumMetadata)
            {
                result[kv.Key] = kv.Value;
            }
            
            return Task.FromResult(result);
        }
    }
}