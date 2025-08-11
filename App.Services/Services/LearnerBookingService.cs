using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.BookingDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Notifications;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace App.Services.Services
{
    public class LearnerBookingService : ILearnerBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly INotificationService _notificationService;

        // Định nghĩa hằng số thời gian tối thiểu (1 giờ)
        private const int MIN_HOURS_BEFORE_BOOKING = 1;

        public LearnerBookingService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider,
            IBackgroundJobClient backgroundJobClient,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
            _backgroundJobClient = backgroundJobClient;
            _notificationService = notificationService;
        }

        #region Private Helpers
        private string GetAuthenticatedLearnerId()
        {
            var learnerId = _currentUserProvider.GetCurrentUserId();
            if (learnerId is null)
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "User is not authenticated.");
            return learnerId;
        }

        private async Task ValidateTutorExistsAsync(string tutorId)
        {
            var tutorExists = await _unitOfWork.GetRepository<Tutor>()
                .ExistEntities()
                .AnyAsync(t => t.UserId == tutorId);

            if (!tutorExists)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    $"Tutor with ID {tutorId} not found.");
        }
        #endregion

        public async Task UpdateTimeSlotRequestsAsync(LearnerTimeSlotRequestDTO request)
        {
            var learnerId = GetAuthenticatedLearnerId();
            await ValidateTutorExistsAsync(request.TutorId);
            if (!string.IsNullOrEmpty(request.LessonId))
            {
                var lessonRepo = _unitOfWork.GetRepository<Lesson>();
                var lesson = await lessonRepo.GetByIdAsync(request.LessonId);
                if (lesson == null)
                    throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Lesson not found.");
            }

            var repo = _unitOfWork.GetRepository<LearnerTimeSlotRequest>();
            var existingRequest = await repo.ExistEntities()
                .FirstOrDefaultAsync(r => r.LearnerId == learnerId && r.TutorId == request.TutorId);

            // If no time slots are provided, it means the user wants to delete their request.
            if (!request.TimeSlots.Any())
            {
                if (existingRequest != null)
                    repo.Delete(existingRequest);
            }
            else
            {
                var requestedSlots = request.TimeSlots.Select(s => new RequestedSlot
                {
                    DayInWeek = s.DayInWeek,
                    SlotIndex = s.SlotIndex
                });

                if (existingRequest != null)
                {
                    var updateFields = existingRequest.Update(request.LessonId, request.ExpectedStartDate, requestedSlots);
                    repo.UpdateFields(existingRequest, updateFields);
                }
                else
                {
                    // No request exists, create a new one.
                    var newRequest = LearnerTimeSlotRequest.Create(learnerId, request.TutorId, request.LessonId, request.ExpectedStartDate, requestedSlots);
                    repo.Insert(newRequest);
                }
            }
            
            await _unitOfWork.SaveAsync();

            await _notificationService.SendToUsersAsync(new()
            {
                Content = new()
                {
                    NotificationPriority = ENotificationPriority.Normal,
                    Title = "PUSH_ON_TUTOR_RECEIVED_TIME_SLOT_REQUEST",
                    Content = "PUSH_ON_TUTOR_RECEIVED_TIME_SLOT_REQUEST_BODY",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        ExpectedStartDate = request.ExpectedStartDate,
                        LessonId = request.LessonId,
                        SenderId = learnerId,
                    }, new JsonSerializerOptions { WriteIndented = false })
                },
                ReceiverUserIds = [request.TutorId]
            });
        }

        public async Task DeleteTimeSlotRequestsAsync(string tutorId)
        {
            var learnerId = GetAuthenticatedLearnerId();
            await ValidateTutorExistsAsync(tutorId);

            var repo = _unitOfWork.GetRepository<LearnerTimeSlotRequest>();
            var requestToDelete = await repo.ExistEntities()
                .FirstOrDefaultAsync(r => r.LearnerId == learnerId && r.TutorId == tutorId);

            if (requestToDelete != null)
            {
                repo.Delete(requestToDelete);
                await _unitOfWork.SaveAsync();
            }
        }

        public async Task<LearnerTimeSlotResponseDTO?> GetTimeSlotRequestByTutorAsync(string tutorId)
        {
            var learnerId = GetAuthenticatedLearnerId();
            await ValidateTutorExistsAsync(tutorId);

            var request = await _unitOfWork.GetRepository<LearnerTimeSlotRequest>()
                .ExistEntities()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.LearnerId == learnerId && r.TutorId == tutorId);
            
            return request == null ? null : LearnerTimeSlotResponseDTO.FromEntity(request);
        }

        public async Task<List<TutorBookingOfferResponse>> GetBookingOffersForLearnerAsync()
        {
            var learnerId = GetAuthenticatedLearnerId();
            return await _unitOfWork.GetRepository<TutorBookingOffer>().ExistEntities()
                .Where(o => o.LearnerId == learnerId)
                .Include(o => o.Tutor).ThenInclude(t => t!.User)
                .Include(o => o.Learner).ThenInclude(l => l!.User)
                .Include(o => o.Lesson)
                .Include(o => o.OfferedSlots)
                .OrderByDescending(o => o.CreatedAt)
                .Select(TutorBookingOfferResponse.Projection)
                .ToListAsync();
        }

        public async Task<TutorBookingOfferResponse> GetBookingOfferByIdForLearnerAsync(string offerId)
        {
            var learnerId = GetAuthenticatedLearnerId();
            var offer = await _unitOfWork.GetRepository<TutorBookingOffer>().ExistEntities()
                .Where(o => o.Id == offerId && o.LearnerId == learnerId)
                .Include(o => o.Tutor).ThenInclude(t => t!.User)
                .Include(o => o.Learner).ThenInclude(l => l!.User)
                .Include(o => o.Lesson)
                .Include(o => o.OfferedSlots)
                .Select(TutorBookingOfferResponse.Projection)
                .FirstOrDefaultAsync();

            if (offer == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Offer not found or you don't have permission to view it.");

            return offer;
        }

        public async Task<List<TutorInfoDTO>> GetAllTimeSlotRequestsForLearnerAsync()
        {
            var learnerId = GetAuthenticatedLearnerId();

            // Pre-fetch all offers for the learner
            var offerLookup = await _unitOfWork.GetRepository<TutorBookingOffer>()
                .ExistEntities()
                .Where(o => o.LearnerId == learnerId)
                .GroupBy(o => o.TutorId)
                .Select(g => new
                {
                    TutorId = g.Key,
                    LatestOfferId = g
                        .OrderByDescending(o => o.CreatedAt)
                        .Select(o => o.Id)
                        .FirstOrDefault()
                })
                .ToDictionaryAsync(x => x.TutorId, x => x.LatestOfferId ?? string.Empty);

            // Fetch requests and process in memory
            var requests = await _unitOfWork.GetRepository<LearnerTimeSlotRequest>()
                .ExistEntities()
                .Include(r => r.Tutor)
                .ThenInclude(t => t.User)
                .Where(r => r.LearnerId == learnerId)
                .ToListAsync();

            return requests
                .Select(r => new TutorInfoDTO
                {
                    TutorId = r.TutorId,
                    TutorName = r.Tutor?.User?.FullName ?? string.Empty,
                    TutorAvatarUrl = r.Tutor?.User?.ProfilePictureUrl ?? string.Empty,
                    LatestRequestTime = r.CreatedAt,
                    TutorBookingOfferId = offerLookup.GetValueOrDefault(r.TutorId, string.Empty)
                })
                .OrderByDescending(x => x.LatestRequestTime)
                .ToList();
        }

        public async Task<BookingResponse> AcceptTutorOfferAsync(AcceptOfferRequest request)
        {
            var learnerId = _currentUserProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(learnerId))
                throw new ErrorException(StatusCodes.Status401Unauthorized, ErrorCode.Unauthorized, "User not authenticated");

            // Get the offer with all related data
            var offer = await _unitOfWork.GetRepository<TutorBookingOffer>()
                .ExistEntities()
                .Include(o => o.OfferedSlots)
                .Include(o => o.Lesson)
                .Include(o => o.Tutor)
                .FirstOrDefaultAsync(o => o.Id == request.OfferId && o.LearnerId == learnerId);

            if (offer == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Offer not found or not intended for this learner");

            if (offer.Lesson == null)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "The lesson associated with this offer no longer exists");

            // Kiểm tra hạn sử dụng của offer
            if (offer.IsExpired())
            {
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    $"This offer has expired. Offers are valid for {offer.ExpirationPeriod.TotalMinutes} minutes after creation or update.");
            }

            // Kiểm tra thời gian của các slot
            var now = DateTime.UtcNow;
            var minAllowedTime = now.AddHours(MIN_HOURS_BEFORE_BOOKING);
            
            foreach (var slot in offer.OfferedSlots)
            {
                var slotStartTime = CalculateSlotStartTime(slot.SlotDateTime.Date, slot.SlotIndex);
                if (slotStartTime <= minAllowedTime)
                {
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        ErrorCode.BadRequest,
                        $"Cannot book slots that start less than {MIN_HOURS_BEFORE_BOOKING} hour(s) from now. Please contact the tutor for rescheduling.");
                }
            }

            // Kiểm tra xem có slot nào đã được book trước đó không
            // Lấy tất cả các BookedSlot của learner
            var existingBookedSlots = await _unitOfWork.GetRepository<BookedSlot>()
                .ExistEntities()
                .Include(bs => bs.Booking)
                .Where(bs => bs.Booking!.LearnerId == learnerId && bs.Status != SlotStatus.Cancelled)
                .Select(bs => new { bs.BookedDate.Date, bs.SlotIndex })
                .ToListAsync();

            // Kiểm tra xem có slot nào trong offer trùng với các slot đã book không
            foreach (var offeredSlot in offer.OfferedSlots)
            {
                var slotDate = offeredSlot.SlotDateTime.Date;
                var slotIndex = offeredSlot.SlotIndex;

                if (existingBookedSlots.Any(bs => bs.Date == slotDate && bs.SlotIndex == slotIndex))
                {
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        ErrorCode.BadRequest,
                        $"You already have a booking for slot {slotIndex} on {slotDate.ToString("dd/MM/yyyy")}. Please check your schedule.");
                }
            }

            // Calculate total price
            var slotCount = offer.OfferedSlots.Count;
            var totalPrice = offer.Lesson.Price * slotCount;

            // Check if learner has enough balance
            var learnerWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == learnerId);

            if (learnerWallet == null)
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Learner wallet not found");

            if (learnerWallet.Balance < totalPrice)
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Insufficient funds in wallet");

            // Create a lesson snapshot
            var lessonSnapshot = LessonSnapshot.CreateFromLesson(offer.Lesson);
            
            return await _unitOfWork.ExecuteInTransactionAsync(async () => {
                // Insert lesson snapshot
                _unitOfWork.GetRepository<LessonSnapshot>().Insert(lessonSnapshot);
                
                // Create booking
                var booking = new Booking
                {
                    TutorId = offer.TutorId,
                    LearnerId = learnerId,
                    Note = $"Booking created from offer {offer.Id}",
                    LessonSnapshotId = lessonSnapshot.Id,
                    OriginalOfferId = offer.Id
                };
                
                booking.TrackCreate(learnerId);
                _unitOfWork.GetRepository<Booking>().Insert(booking);
                
                // Create booked slots and held funds
                var bookedSlots = new List<BookedSlot>();
                var heldFunds = new List<HeldFund>();
                var escrowWallet = await GetEscrowWalletAsync();
                
                foreach (var offeredSlot in offer.OfferedSlots)
                {
                    var slotDate = offeredSlot.SlotDateTime.Date;
                    var slotStartTime = CalculateSlotStartTime(slotDate, offeredSlot.SlotIndex);
                    var slotEndTime = slotStartTime.AddMinutes(offer.Lesson.DurationInMinutes);
                    var releaseTime = slotEndTime.AddHours(24); 
                    
                    // Create held fund
                    var heldFund = HeldFund.CreateForBooking(string.Empty, offer.Lesson.Price, releaseTime);
                    _unitOfWork.GetRepository<HeldFund>().Insert(heldFund);
                    heldFunds.Add(heldFund);
                    
                    // Create booked slot
                    var bookedSlot = new BookedSlot
                    {
                        BookingId = booking.Id,
                        BookedDate = slotDate, 
                        SlotIndex = offeredSlot.SlotIndex,
                        Status = SlotStatus.AwaitingConfirmation,
                        HeldFundId = heldFund.Id
                    };
                    
                    bookedSlot.TrackCreate(learnerId);
                    _unitOfWork.GetRepository<BookedSlot>().Insert(bookedSlot);
                    bookedSlots.Add(bookedSlot);
                    
                    heldFund.BookedSlotId = bookedSlot.Id;
                    _unitOfWork.GetRepository<HeldFund>().UpdateFields(heldFund, h => h.BookedSlotId!); 
                }
                
                var transaction = Transaction.CreatePaymentTransaction(
                    learnerWallet.Id,
                    escrowWallet.Id,
                    totalPrice,
                    booking.Id,
                    $"Payment for booking {booking.Id} with {slotCount} slots held in escrow"
                );
                
                _unitOfWork.GetRepository<Transaction>().Insert(transaction);
                
                // Update wallet balances
                var learnerUpdateFields = learnerWallet.SubtractBalance(totalPrice);
                _unitOfWork.GetRepository<Wallet>().UpdateFields(learnerWallet, learnerUpdateFields);
                
                var escrowUpdateFields = escrowWallet.AddBalance(totalPrice);
                _unitOfWork.GetRepository<Wallet>().UpdateFields(escrowWallet, escrowUpdateFields);
                
                _unitOfWork.GetRepository<TutorBookingOffer>().Delete(offer);
                
                // Xóa tất cả LearnerTimeSlotRequest liên quan đến tutor này
                var timeSlotRequests = await _unitOfWork.GetRepository<LearnerTimeSlotRequest>()
                    .ExistEntities()
                    .Where(r => r.LearnerId == learnerId && r.TutorId == offer.TutorId)
                    .ToListAsync();
                
                foreach (var request in timeSlotRequests)
                {
                    _unitOfWork.GetRepository<LearnerTimeSlotRequest>().Delete(request);
                }
                
                await _unitOfWork.SaveAsync();
                
                // Schedule release of funds using Hangfire with injected client
                foreach (var heldFund in heldFunds)
                {
                    if (heldFund.ReleaseAt.HasValue)
                        _backgroundJobClient.Schedule<IPaymentProcessingService>(
                            service => service.ProcessHeldFundReleaseAsync(heldFund.Id),
                            heldFund.ReleaseAt.Value - DateTime.UtcNow
                        );
                }

                await _notificationService.SendToUsersAsync(new()
                {
                    Content = new()
                    {
                        NotificationPriority = ENotificationPriority.Normal,
                        Title = "PUSH_ON_LEARNER_ACCEPT_OFFER",
                        Content = "PUSH_ON_LEARNER_ACCEPT_OFFER_BODY",
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            Id = booking.Id,
                            LessonName = lessonSnapshot.Name,
                            SenderId = learnerId,
                        }),
                    },
                    ReceiverUserIds = [booking.TutorId]
                });

                // Map to response
                return new BookingResponse
                {
                    Id = booking.Id,
                    TutorId = booking.TutorId,
                    LearnerId = booking.LearnerId,
                    LessonName = lessonSnapshot.Name,
                    TotalPrice = totalPrice,
                    SlotCount = slotCount,
                    BookedSlots = bookedSlots.Select(bs => new BookedSlotDTO
                    {
                        Id = bs.Id,
                        BookedDate = bs.BookedDate,
                        SlotIndex = bs.SlotIndex,
                        Status = bs.Status
                    }).ToList()
                };
            });
        }

        public async Task<TutorBookingOfferResponse> RejectBookingOfferAsync(string offerId)
        {
            var learnerId = GetAuthenticatedLearnerId();
            
            // Tìm offer
            var offerRepo = _unitOfWork.GetRepository<TutorBookingOffer>();
            var offer = await offerRepo.ExistEntities()
                .Include(o => o.Tutor).ThenInclude(t => t!.User)
                .Include(o => o.Learner).ThenInclude(l => l!.User)
                .Include(o => o.Lesson)
                .Include(o => o.OfferedSlots)
                .FirstOrDefaultAsync(o => o.Id == offerId && o.LearnerId == learnerId);

            if (offer == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Offer not found or you don't have permission to reject it.");

            // Kiểm tra nếu offer đã hết hạn
            if (offer.IsExpired())
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Cannot reject an expired offer.");

            // Đánh dấu offer là đã từ chối
            var updateFields = offer.MarkAsRejected();
            if (updateFields.Any())
            {
                offerRepo.UpdateFields(offer, updateFields);
                await _unitOfWork.SaveAsync();
                
                // Gửi thông báo cho tutor
                // await _notificationService.SendToUsersAsync(new()
                // {
                //     Content = new()
                //     {
                //         NotificationPriority = ENotificationPriority.Normal,
                //         Title = "PUSH_ON_LEARNER_REJECT_OFFER",
                //         Content = "PUSH_ON_LEARNER_REJECT_OFFER_BODY",
                //         AdditionalData = JsonSerializer.Serialize(new
                //         {
                //             OfferId = offer.Id,
                //             LessonId = offer.LessonId,
                //             SenderId = learnerId,
                //         }),
                //     },
                //     ReceiverUserIds = [offer.TutorId]
                // });
            }

            return await offerRepo.ExistEntities()
                .Where(o => o.Id == offerId)
                .Include(o => o.Tutor).ThenInclude(t => t!.User)
                .Include(o => o.Learner).ThenInclude(l => l!.User)
                .Include(o => o.Lesson)
                .Include(o => o.OfferedSlots)
                .Select(TutorBookingOfferResponse.Projection)
                .FirstAsync();
        }

        private async Task<Wallet> GetSystemWalletAsync()
        {
            var systemWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.Type == WalletType.System);
                
            if (systemWallet == null)
                throw new ErrorException(StatusCodes.Status500InternalServerError, ErrorCode.ServerError, "System wallet not found");
                
            return systemWallet;
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

        // Helper method to calculate actual start time from date and slot index
        private DateTime CalculateSlotStartTime(DateTime date, int slotIndex)
        {
            // Giả sử mỗi slot là 30 phút và slot đầu tiên (index 0) bắt đầu lúc 8:00 sáng
            // Bạn có thể điều chỉnh logic này theo cách tính slot của hệ thống của bạn
            int hoursToAdd = slotIndex / 2; // Mỗi giờ có 2 slot (30 phút mỗi slot)
            int minutesToAdd = (slotIndex % 2) * 30; // 0 hoặc 30 phút
            
            return date.AddHours(8 + hoursToAdd).AddMinutes(minutesToAdd);
        }
    }
}
