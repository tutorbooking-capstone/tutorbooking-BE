using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.Core.Utils;
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
    public class TutorBookingService : ITutorBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly INotificationService _notificationService;

        public TutorBookingService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
            _notificationService = notificationService;
        }

        #region Private Helpers
        private string GetAuthenticatedTutorId()
        {
            var tutorId = _currentUserProvider.GetCurrentUserId();
            if (tutorId is null)
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "User is not authenticated.");
            return tutorId;
        }

        private async Task ValidateLearnerExistsAsync(string learnerId)
        {
            var learnerExists = await _unitOfWork.GetRepository<Learner>()
                .ExistEntities()
                .AnyAsync(l => l.UserId == learnerId);
            if (!learnerExists)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    $"Learner with ID {learnerId} not found.");
        }

        private async Task<Lesson> ValidateAndGetLessonAsync(string lessonId, string tutorId)
        {
            var lesson = await _unitOfWork.GetRepository<Lesson>()
                .ExistEntities()
                .FirstOrDefaultAsync(l => l.Id == lessonId && l.TutorId == tutorId);
            if (lesson == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    $"Lesson with ID {lessonId} not found or does not belong to the authenticated tutor.");
            return lesson;
        }
        #endregion

        public async Task<List<LearnerInfoDTO>> GetAllTimeSlotRequestsForTutorAsync()
        {
            var tutorId = GetAuthenticatedTutorId();

            return await _unitOfWork.GetRepository<LearnerTimeSlotRequest>()
                .ExistEntities()
                .Include(r => r.Learner)
                .ThenInclude(l => l!.User)
                .Where(r => r.TutorId == tutorId)
                .Select(r => new LearnerInfoDTO
                {
                    LearnerId = r.LearnerId,
                    LearnerName = r.Learner!.User!.FullName ?? "",
                    HasUnviewed = !r.LastViewedAt.HasValue,
                    LatestRequestTime = r.CreatedAt
                })
                .OrderByDescending(x => x.LatestRequestTime)
                .ToListAsync();
        }

        public async Task<LearnerTimeSlotResponseDTO?> GetTimeSlotRequestByLearnerAsync(string learnerId)
        {
            var tutorId = GetAuthenticatedTutorId();
            var repo = _unitOfWork.GetRepository<LearnerTimeSlotRequest>();
            
            var request = await repo.ExistEntities()
                .FirstOrDefaultAsync(r => r.LearnerId == learnerId && r.TutorId == tutorId);

            if (request == null) return null;

            if (!request.LastViewedAt.HasValue)
            {
                var updateFields = request.MarkAsViewed();
                repo.UpdateFields(request, updateFields);
                await _unitOfWork.SaveAsync();
            }

            return LearnerTimeSlotResponseDTO.FromEntity(request);
        }

        public async Task<TutorBookingOfferResponse> CreateBookingOfferAsync(CreateTutorBookingOfferRequest request)
        {
            var tutorId = GetAuthenticatedTutorId();
            await ValidateLearnerExistsAsync(request.LearnerId);
            var lesson = await ValidateAndGetLessonAsync(request.LessonId, tutorId);

            var offerRepo = _unitOfWork.GetRepository<TutorBookingOffer>();

            var offeredSlots = request.OfferedSlots.Select(s => (s.SlotDateTime, s.SlotIndex)).ToList();
            var newOffer = TutorBookingOffer.Create(
                tutorId, 
                request.LearnerId, 
                request.LessonId, 
                offeredSlots
            );

            offerRepo.Insert(newOffer);
            await _unitOfWork.SaveAsync();

            var createdOffer = await offerRepo.ExistEntities()
                .Include(o => o.Tutor).ThenInclude(t => t!.User)
                .Include(o => o.Learner).ThenInclude(l => l!.User)
                .Include(o => o.Lesson)
                .Include(o => o.OfferedSlots)
                .Where(o => o.Id == newOffer.Id)
                .Select(TutorBookingOfferResponse.Projection)
                .FirstAsync();

            await _notificationService.SendToUsersAsync(new()
            {
                Content = new()
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = "Bạn có đề xuất mới",
                    Content = "Một gia sư đã tạo đề xuất mới cho bạn",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Type = "NewBookingOfferReceived",
                        SenderId = tutorId,
                        OfferId = createdOffer.Id,
                        LessonId = createdOffer.LessonId
                    })
                },
                ReceiverUserIds = [request.LearnerId]
            });

            var expirationTime = (newOffer.UpdatedAt ?? newOffer.CreatedAt).Add(newOffer.ExpirationPeriod);
            HangfireConfig.ScheduleOfferExpirationJob(newOffer.Id, expirationTime);

            return createdOffer;
        }

        public async Task<TutorBookingOfferResponse> UpdateBookingOfferAsync(string offerId, UpdateTutorBookingOfferRequest request)
        {
            var tutorId = GetAuthenticatedTutorId();
            var offerRepo = _unitOfWork.GetRepository<TutorBookingOffer>();
            var slotRepo = _unitOfWork.GetRepository<OfferedSlot>();

            var offer = await offerRepo.ExistEntities()
                .Include(o => o.OfferedSlots)
                .Include(o => o.Lesson)
                .FirstOrDefaultAsync(o => o.Id == offerId && o.TutorId == tutorId);

            if (offer == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Offer not found or you don't have permission to update it.");

            if (offer.Lesson == null)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Cannot update an offer with a deleted lesson.");

            // Remove old slots
            slotRepo.DeleteRange(offer.OfferedSlots);

            // Add new slots
            offer.OfferedSlots = request.OfferedSlots.Select(s => new OfferedSlot
            {
                TutorBookingOfferId = offer.Id,
                SlotDateTime = s.SlotDateTime,
                SlotIndex = s.SlotIndex,
            }).ToList();
            offer.UpdatedAt = DateTime.UtcNow;
            
            offerRepo.UpdateFields(offer, o => o.UpdatedAt!);
            slotRepo.InsertRange(offer.OfferedSlots);
            
            await _unitOfWork.SaveAsync();

            await _notificationService.SendToUsersAsync(new()
            {
                Content = new()
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = "Nhận được cập nhật đề xuất",
                    Content = "Một gia sư đã cập nhật lại đề xuất buổi học cho bạn",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Type = "BookingOfferUpdateReceived",
                        SenderId = tutorId,
                        OfferId = offer.Id,
                        LessonId = offer.LessonId,
                    })
                },
                ReceiverUserIds = [offer.LearnerId]
            });

            return await GetBookingOfferByIdForTutorAsync(offerId);
        }

        public async Task DeleteBookingOfferAsync(string offerId)
        {
            var tutorId = GetAuthenticatedTutorId();
            var offerRepo = _unitOfWork.GetRepository<TutorBookingOffer>();
            var slotRepo = _unitOfWork.GetRepository<OfferedSlot>();
            
            var offer = await offerRepo.ExistEntities()
                .Include(o => o.OfferedSlots)
                .FirstOrDefaultAsync(o => o.Id == offerId && o.TutorId == tutorId);

            if (offer == null)
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Offer not found or you don't have permission to delete it.");

            if (offer.OfferedSlots?.Any() == true)
                slotRepo.DeleteRange(offer.OfferedSlots);
                
            offerRepo.Delete(offer, isSoftDelete: false);
            await _unitOfWork.SaveAsync();
        }

        public async Task<List<TutorBookingOfferResponse>> GetAllBookingOffersByTutorAsync()
        {
            var tutorId = GetAuthenticatedTutorId();
            return await _unitOfWork.GetRepository<TutorBookingOffer>().ExistEntities()
                .Where(o => o.TutorId == tutorId)
                .Include(o => o.Tutor).ThenInclude(t => t!.User)
                .Include(o => o.Learner).ThenInclude(l => l!.User)
                .Include(o => o.Lesson)
                .Include(o => o.OfferedSlots)
                .OrderByDescending(o => o.CreatedAt)
                .Select(TutorBookingOfferResponse.Projection)
                .ToListAsync();
        }

        public async Task<TutorBookingOfferResponse> GetBookingOfferByIdForTutorAsync(string offerId)
        {
            var tutorId = GetAuthenticatedTutorId();
            var offer = await _unitOfWork.GetRepository<TutorBookingOffer>().ExistEntities()
                .Where(o => o.Id == offerId && o.TutorId == tutorId)
                .Include(o => o.Tutor).ThenInclude(t => t!.User)
                .Include(o => o.Learner).ThenInclude(l => l!.User)
                .Include(o => o.Lesson)
                .Include(o => o.OfferedSlots)
                .Select(TutorBookingOfferResponse.Projection)
                .FirstOrDefaultAsync();

            if (offer == null)
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Offer not found or you don't have permission to view it.");

            return offer;
        }

        public async Task MarkSlotAsCompletedAsync(string bookedSlotId)
        {
            var tutorId = GetAuthenticatedTutorId();

            var bookedSlotRepo = _unitOfWork.GetRepository<BookedSlot>();
            var bookedSlot = await bookedSlotRepo.ExistEntities()
                .Include(bs => bs.Booking)
                .ThenInclude(b => b!.LessonSnapshot)
                .Include(bs => bs.Booking)
                .ThenInclude(b => b!.Learner)
                .ThenInclude(l => l!.User)
                .FirstOrDefaultAsync(bs => bs.Id == bookedSlotId);

            // Rule 1: Check if the slot exists
            if (bookedSlot == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Booked slot not found.");

            // Rule 2: Authorization: Check if the slot belongs to the authenticated tutor
            if (bookedSlot.Booking?.TutorId != tutorId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden, 
                    ErrorCode.Forbidden, 
                    "You are not authorized to modify this booked slot.");

            if (bookedSlot.Status != SlotStatus.Pending)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "SLOT_STATUS_MUST_BE_PENDING");
            try
            {
                var updateFields = bookedSlot.ModifySlotStatus(tutorId, SlotStatus.AwaitingPayout);

                if (updateFields.Any())
                {
                    bookedSlotRepo.UpdateFields(bookedSlot, updateFields);
                    await _unitOfWork.SaveAsync();

                    await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest()
                    {
                        Content = new()
                        {
                            NotificationPriority = ENotificationPriority.Normal,
                            Title = "Buổi học đã hoàn thành",
                            Content = $"Buổi học {bookedSlot.GetSlotStartTime:hh:mm dd/MM/yyyy} đã hoàn thành.",
                            AdditionalData = JsonSerializer.Serialize(new
                            {
                                Type = "BookedSlotCompleted",
                                SenderId = tutorId,
                                BookedSlotId = bookedSlotId
                            }),
                        },
                        ReceiverUserIds = [bookedSlot.Booking!.LearnerId]
                    });
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    ex.Message);
            }

            // Note: Fund release is handled by a scheduled job. No immediate action here.
        }

        public async Task<BookingResponse> CancelBookingAsync(string bookingId, string? cancellationReason = null)
        {
            var tutorId = GetAuthenticatedTutorId();
            
            // Get booking with all related data
            var booking = await _unitOfWork.GetRepository<Booking>()
                .ExistEntities()
                .Include(b => b.BookedSlots!).ThenInclude(bs => bs.HeldFund)
                .Include(b => b.LessonSnapshot)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.TutorId == tutorId);
                
            if (booking == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Booking not found or you don't have permission to cancel it.");
            
            // Check if booking has any completed slots
            if (booking.BookedSlots != null && booking.BookedSlots.Any(bs => bs.Status == SlotStatus.Completed))
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Cannot cancel booking with completed slots.");
            
            // Get all slots that can be cancelled (Pending status)
            var slotsToCancel = booking.BookedSlots!
                .Where(bs => bs.Status == SlotStatus.Pending)
                .OrderBy(bs => bs.BookedDate)
                .ThenBy(bs => bs.SlotIndex)
                .ToList();
            
            if (!slotsToCancel.Any())
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "No pending slots to cancel.");
            
            // For tutor cancellations, learner gets 100% refund regardless of timing
            var learnerWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == booking.LearnerId);
            
            if (learnerWallet == null)
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    ErrorCode.ServerError,
                    "Learner wallet not found");
            
            var escrowWallet = await GetEscrowWalletAsync();
            
            return await _unitOfWork.ExecuteInTransactionAsync(async () => {
                foreach (var slot in slotsToCancel)
                {
                    // Update slot status
                    var updateFields = slot.MarkAsCancelled(tutorId);
                    _unitOfWork.GetRepository<BookedSlot>().UpdateFields(slot, updateFields);
                    
                    // Process held funds - full refund to learner
                    if (slot.HeldFund != null)
                    {
                        var refundAmount = slot.HeldFund.Amount;
                        
                        // Refund to learner
                        var learnerTransaction = Transaction.CreatePaymentTransaction(
                            escrowWallet.Id,
                            learnerWallet.Id,
                            refundAmount,
                            slot.Id,
                            $"Hoàn tiền đầy đủ do gia sư huỷ lịch học"
                        );
                        _unitOfWork.GetRepository<Transaction>().Insert(learnerTransaction);
                        
                        var learnerWalletUpdateFields = learnerWallet.AddBalance(refundAmount);
                        _unitOfWork.GetRepository<Wallet>().UpdateFields(learnerWallet, learnerWalletUpdateFields);
                        
                        var escrowUpdateFields = escrowWallet.SubtractBalance(refundAmount);
                        _unitOfWork.GetRepository<Wallet>().UpdateFields(escrowWallet, escrowUpdateFields);
                        
                        // Update held fund status
                        var heldFundUpdateFields = slot.HeldFund.UpdateStatus(HeldFundStatus.RefundedToLearner);
                        _unitOfWork.GetRepository<HeldFund>().UpdateFields(slot.HeldFund, heldFundUpdateFields);
                    }
                }
                
                // Update booking status if all slots are cancelled
                if (!booking.BookedSlots!.Any(bs => bs.Status != SlotStatus.Cancelled && bs.Status != SlotStatus.CancelledDisputed))
                {
                    var bookingUpdateFields = booking.UpdateStatus(BookingStatus.Cancelled, tutorId);
                    _unitOfWork.GetRepository<Booking>().UpdateFields(booking, bookingUpdateFields);
                }
                
                await _unitOfWork.SaveAsync();
                
                // Send notification to learner
                await _notificationService.SendToUsersAsync(new()
                {
                    Content = new()
                    {
                        NotificationPriority = ENotificationPriority.Normal,
                        Title = "Lịch học đã bị huỷ",
                        Content = "Một gia sư đã huỷ lịch học của bạn.",
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            Type = "TutorCancelledBooking",
                            BookingId = booking.Id,
                            LessonName = booking.LessonSnapshot?.Name,
                            SenderId = tutorId,
                            CancellationReason = cancellationReason ?? "No reason provided"
                        })
                    },
                    ReceiverUserIds = [booking.LearnerId]
                });
                
                return BookingResponse.FromEntity(
                    booking, 
                    booking.LessonSnapshot!, 
                    booking.BookedSlots!.ToList(),
                    booking.BookedSlots!.Sum(bs => bs.HeldFund?.Amount ?? 0)
                );
            });
        }

        private async Task<Wallet> GetEscrowWalletAsync()
        {
            var escrowWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.Type == WalletType.Escrow);
                    
            if (escrowWallet == null)
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError, 
                    ErrorCode.ServerError, 
                    "Escrow wallet not found");
            
            return escrowWallet;
        }
    }
}