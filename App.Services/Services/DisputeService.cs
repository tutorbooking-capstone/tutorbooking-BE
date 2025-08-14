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

namespace App.Services.Services
{
    public class DisputeService : IDisputeService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly IWalletService _walletService;
        private readonly INotificationService _notificationService;

        public DisputeService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider,
            IWalletService walletService,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
            _walletService = walletService;
            _notificationService = notificationService;
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

        private async Task<Booking> GetAndValidateBookingAsync(string bookingId, string learnerId)
        {
            var booking = await _unitOfWork.GetRepository<Booking>()
                .GetQueryable()
                .Include(b => b.Tutor)
                .Include(b => b.Learner)
                .Include(b => b.BookedSlots)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.LearnerId == learnerId);
                
            if (booking == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Không tìm thấy thông tin đặt chỗ");
                
            if (booking.Status == BookingStatus.Cancelled)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Đặt chỗ này đã bị hủy");
                
            if (booking.Status == BookingStatus.Disputed)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Đặt chỗ này đang trong quá trình tranh chấp");
                
            return booking;
        }

        private async Task<BookingDispute> GetAndValidateDisputeAsync(string disputeId, bool checkEscalated = false)
        {
            var dispute = await _unitOfWork.GetRepository<BookingDispute>()
                .GetQueryable()
                .Include(d => d.Booking)
                .Include(d => d.Learner)
                .Include(d => d.Tutor)
                .FirstOrDefaultAsync(d => d.Id == disputeId);
                
            if (dispute == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Không tìm thấy thông tin khiếu nại");
                
            if (checkEscalated && dispute.Status != DisputeStatus.AwaitingStaffReview)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Trạng thái khiếu nại không hợp lệ");
                
            return dispute;
        }

        private async Task<List<BookedSlot>> GetUpcomingSlotsAsync(string bookingId)
        {
            return await _unitOfWork.GetRepository<BookedSlot>()
                .GetQueryable()
                .Where(bs => bs.BookingId == bookingId && 
                        bs.BookedDate > DateTime.UtcNow && 
                        bs.Status != SlotStatus.Cancelled && 
                        bs.Status != SlotStatus.CancelledDisputed &&
                        bs.Status != SlotStatus.Completed)  
                .ToListAsync();
        }

        private async Task CancelUpcomingSlotsAsync(string bookingId, string disputeId)
        {
            var slots = await GetUpcomingSlotsAsync(bookingId);
            var userId = GetAuthenticatedUserId();
            var bookedSlotRepo = _unitOfWork.GetRepository<BookedSlot>();
            var heldFundRepo = _unitOfWork.GetRepository<HeldFund>();
            
            foreach (var slot in slots)
            {
                var updateProperties = slot.MarkAsCancelledDisputed(disputeId, userId);
                bookedSlotRepo.UpdateFields(slot, updateProperties.ToArray());
                
                if (!string.IsNullOrEmpty(slot.HeldFundId))
                {
                    var heldFund = await heldFundRepo.GetByIdAsync(slot.HeldFundId);
                    if (heldFund != null)
                    {
                        var updateFundProperties = heldFund.UpdateStatus(HeldFundStatus.Disputed);
                        heldFundRepo.UpdateFields(heldFund, updateFundProperties.ToArray());
                    }
                }
            }
        }

        private async Task ProcessDisputeResolution(BookingDispute dispute, DisputeResolution resolution)
        {
            // Get all affected slots
            var affectedSlots = await _unitOfWork.GetRepository<BookedSlot>()
                .GetQueryable()
                .Where(bs => bs.DisputeId == dispute.Id)
                .ToListAsync();

            var bookedSlotRepo = _unitOfWork.GetRepository<BookedSlot>();
                
            // Process funds based on resolution
            foreach (var slot in affectedSlots)
            {
                if (string.IsNullOrEmpty(slot.HeldFundId))
                    continue;
                    
                var heldFund = await _unitOfWork.GetRepository<HeldFund>().GetByIdAsync(slot.HeldFundId);
                if (heldFund == null)
                    continue;
                
                switch (resolution)
                {
                    case DisputeResolution.LearnerWithdrew:
                        // Return to original status - no escrow
                        var updateProperties = slot.UpdateStatus(SlotStatus.Pending, GetAuthenticatedUserId());
                        bookedSlotRepo.UpdateFields(slot, updateProperties);
                        break;
                        
                    case DisputeResolution.TutorNoResponse:
                    case DisputeResolution.StaffLearnerWin:
                        // Refund to learner
                        await _walletService.RefundHeldFundToLearnerAsync(heldFund.Id);
                        break;
                        
                    case DisputeResolution.StaffTutorWin:
                        // 5% to tutor, 95% to learner
                        await _walletService.PartialRefundForDisputeAsync(
                            heldFund.Id, 
                            0.05m, // 5% to tutor
                            dispute.BookingId);
                        break;
                        
                    case DisputeResolution.StaffDraw:
                        // Full refund to learner, no penalty to tutor
                        await _walletService.RefundHeldFundToLearnerAsync(heldFund.Id);
                        break;
                }
            }
            
            // Update booking status
            if (dispute.Booking != null)
            {
                BookingStatus newStatus;
                
                switch (resolution)
                {
                    case DisputeResolution.LearnerWithdrew:
                        newStatus = BookingStatus.Confirmed;
                        break;
                    case DisputeResolution.TutorNoResponse:
                    case DisputeResolution.StaffLearnerWin:
                    case DisputeResolution.StaffTutorWin:
                    case DisputeResolution.StaffDraw:
                        newStatus = BookingStatus.Cancelled;
                        break;
                    default:
                        newStatus = BookingStatus.Confirmed;
                        break;
                }

                var bookingRepo = _unitOfWork.GetRepository<Booking>();
                
                var updateProperties = dispute.Booking.UpdateStatus(newStatus, GetAuthenticatedUserId());
                bookingRepo.UpdateFields(dispute.Booking, updateProperties.ToArray());
                
                var clearDisputeProperties = dispute.Booking.ClearCurrentDispute();
                bookingRepo.UpdateFields(dispute.Booking, clearDisputeProperties.ToArray());
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
                        Type = "Dispute",
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
                        Type = "Dispute",
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
                    tutorMessage = $"Học viên đã rút lại khiếu nại {dispute.CaseNumber}. Booking tiếp tục như bình thường.";
                    learnerMessage = $"Bạn đã rút lại khiếu nại {dispute.CaseNumber}. Booking tiếp tục như bình thường.";
                    break;
                    
                case DisputeResolution.TutorNoResponse:
                    tutorMessage = $"Bạn không phản hồi khiếu nại {dispute.CaseNumber} đúng hạn. Hệ thống đã hủy các buổi học còn lại và hoàn tiền cho học viên.";
                    learnerMessage = $"Gia sư không phản hồi khiếu nại {dispute.CaseNumber}. Hệ thống đã hủy các buổi học còn lại và hoàn tiền cho bạn.";
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
                    tutorMessage = $"Nhân viên hệ thống đã xử lý khiếu nại {dispute.CaseNumber} và xác định đây là hòa. Bạn không bị trừ uy tín, học viên sẽ được hoàn tiền.";
                    learnerMessage = $"Nhân viên hệ thống đã xử lý khiếu nại {dispute.CaseNumber} và xác định đây là hòa. Bạn sẽ được hoàn lại toàn bộ tiền.";
                    staffMessage = $"Đã xử lý khiếu nại {dispute.CaseNumber} với kết quả hòa.";
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

        private async Task<decimal> CalculateDisputedAmount(List<BookedSlot> slots)
        {
            if (slots == null || !slots.Any())
                return 0;
                
            decimal total = 0;
            
            foreach (var slot in slots)
            {
                // Get HeldFund amount if available
                if (!string.IsNullOrEmpty(slot.HeldFundId))
                {
                    var heldFund = await _unitOfWork.GetRepository<HeldFund>().GetByIdAsync(slot.HeldFundId);
                    if (heldFund != null)
                    {
                        total += heldFund.Amount;
                        continue;
                    }
                }
                
                // Otherwise try to get from booking's lesson
                if (slot.Booking?.LessonSnapshot != null)
                {
                    // Use the snapshot price
                    var lessonSnapshot = await _unitOfWork.GetRepository<LessonSnapshot>().GetByIdAsync(slot.Booking.LessonSnapshotId!);
                    if (lessonSnapshot != null)
                    {
                        total += lessonSnapshot.Price;
                    }
                }
            }
            
            return total;
        }
        #endregion

        #region Learner Operations
        public async Task<BookingDisputeResponse> CreateDisputeAsync(CreateDisputeRequest request)
        {
            var learnerId = await GetAuthenticatedLearnerIdAsync();
            var booking = await GetAndValidateBookingAsync(request.BookingId, learnerId);
            
            // Check if there's already an active dispute
            if (booking.Status == BookingStatus.DisputeRequested)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Đặt chỗ này đã có khiếu nại đang chờ xử lý");
            
            // Create evidence URLs JSON if provided
            string? evidenceUrlsJson = null;
            if (request.EvidenceUrls != null && request.EvidenceUrls.Any())
                evidenceUrlsJson = JsonSerializer.Serialize(request.EvidenceUrls);
            
            // Create dispute
            var dispute = BookingDispute.CreateDispute(
                request.BookingId,
                learnerId,
                booking.TutorId,
                request.Reason,
                evidenceUrlsJson);

            var disputeRepo = _unitOfWork.GetRepository<BookingDispute>();
            var bookingRepo = _unitOfWork.GetRepository<Booking>();
                
            // Add to database
            disputeRepo.Insert(dispute);
            
            // Update booking status
            var updateProperties = booking.UpdateStatus(BookingStatus.DisputeRequested, learnerId);
            bookingRepo.UpdateFields(booking, updateProperties.ToArray());
            
            // Set current dispute
            var disputeProperties = booking.SetCurrentDispute(dispute.Id);
            bookingRepo.UpdateFields(booking, disputeProperties.ToArray());
            
            await _unitOfWork.SaveAsync();
            
            // Send notifications
            await SendDisputeCreatedNotificationsAsync(dispute);
            
            return await GetDisputeResponseAsync(dispute.Id);
        }

        public async Task<BookingDisputeResponse> WithdrawDisputeAsync(WithdrawDisputeRequest request)
        {
            var learnerId = await GetAuthenticatedLearnerIdAsync();
            var dispute = await GetAndValidateDisputeAsync(request.DisputeId);
            
            // Verify learner owns this dispute
            if (dispute.LearnerId != learnerId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden, 
                    ErrorCode.Forbidden, 
                    "Bạn không có quyền rút lại khiếu nại này");
                
            // Can only withdraw during reconciliation
            if (dispute.Status != DisputeStatus.PendingReconciliation)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Không thể rút lại khiếu nại ở trạng thái hiện tại");
                
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
            var dispute = await GetAndValidateDisputeAsync(disputeId);
            
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
            var dispute = await GetAndValidateDisputeAsync(request.DisputeId);
            
            // Verify tutor owns this dispute
            if (dispute.TutorId != tutorId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden, 
                    ErrorCode.Forbidden, 
                    "Bạn không có quyền phản hồi khiếu nại này");
                
            // Can only respond during reconciliation
            if (dispute.Status != DisputeStatus.PendingReconciliation)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Không thể phản hồi khiếu nại ở trạng thái hiện tại");
                
            // Check if reconciliation period expired
            if (dispute.IsReconciliationExpired())
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Thời gian phản hồi đã hết");

            var disputeRepo = _unitOfWork.GetRepository<BookingDispute>();
                
            // Update dispute with response
            var updateProperties = dispute.AddTutorResponse(request.Response);
            disputeRepo.UpdateFields(dispute, updateProperties.ToArray());
            
            // Get booking and escalate
            var booking = await _unitOfWork.GetRepository<Booking>().GetByIdAsync(dispute.BookingId);
            if (booking != null)
            {
                var updateBookingProperties = booking.UpdateStatus(BookingStatus.Disputed, tutorId);
                _unitOfWork.GetRepository<Booking>().UpdateFields(booking, updateBookingProperties.ToArray());
            }
            
            // Escalate to staff review
            var staffId = await GetSystemStaffIdAsync();
            var escalateProperties = dispute.EscalateToStaff(staffId);
            disputeRepo.UpdateFields(dispute, escalateProperties.ToArray());
            
            // Cancel upcoming slots and move funds to escrow
            await CancelUpcomingSlotsAsync(dispute.BookingId, dispute.Id);
            
            await _unitOfWork.SaveAsync();
            
            // Notify staff and parties
            await NotifyDisputeEscalatedAsync(dispute);
            
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
            var dispute = await GetAndValidateDisputeAsync(disputeId);
            
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
            var dispute = await GetAndValidateDisputeAsync(request.DisputeId, checkEscalated: true);
            EnsureHasManagerialAccess("Bạn không có quyền xử lý khiếu nại này");
            
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
            var dispute = await GetAndValidateDisputeAsync(disputeId);
            
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
                .Include(d => d.Booking)
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
                .Include(d => d.Booking)
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
                .Include(d => d.Booking)
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
            
            // Get affected slots
            var affectedSlots = await _unitOfWork.GetRepository<BookedSlot>()
                .GetQueryable()
                .Include(bs => bs.HeldFund)
                .Where(bs => bs.BookingId == dispute.BookingId && 
                        (bs.Status == SlotStatus.CancelledDisputed || bs.DisputeId == dispute.Id))
                .ToListAsync();
                
            var slotResponses = affectedSlots.Select(s => new BookedSlotDTO
            {
                Id = s.Id,
                BookedDate = s.BookedDate,
                SlotIndex = s.SlotIndex,
                Status = s.Status
            }).ToList();
            
            // Calculate disputed amount
            var disputedAmount = await CalculateDisputedAmount(affectedSlots);
            
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