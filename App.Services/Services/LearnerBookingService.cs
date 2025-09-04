using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.BookingDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Notifications;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Infras;
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
        private readonly INotificationService _notificationService;

        private const int MIN_HOURS_BEFORE_BOOKING = 24;
        private const int RELEASE_TIME_OFFSET_HOURS = 3*24;

        public LearnerBookingService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
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
                    Title = "Nhận được yêu cầu đặt lịch",
                    Content = $"Một học viên đã gửi yêu cầu đặt lịch cho ngày {request.ExpectedStartDate:dd/MM/yyyy}.",
                    AdditionalData = JsonSerializer.Serialize(new
                    {
                        Type = "TimeSlotRequestReceived",
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

        private async Task<(
            TutorBookingOffer offer, 
            Wallet learnerWallet, 
            decimal totalPrice)> 
            ValidateOfferAndBookingConditionsAsync(string offerId, string learnerId)
        {
            // Get the offer with all related data
            var offer = await _unitOfWork.GetRepository<TutorBookingOffer>()
                .ExistEntities()
                .Include(o => o.OfferedSlots)
                .Include(o => o.Lesson)
                .Include(o => o.Tutor)
                .FirstOrDefaultAsync(o => o.Id == offerId && o.LearnerId == learnerId);

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

            if (offer.IsExpired)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    $"This offer has expired. Offers are valid for {offer.ExpirationPeriod.TotalMinutes} minutes after creation or update.");

            var existingBookedSlots = await _unitOfWork.GetRepository<BookedSlot>()
                .ExistEntities()
                .Include(bs => bs.Booking)
                .Where(bs => bs.Booking!.LearnerId == learnerId && 
                        (bs.Status == SlotStatus.Pending))
                .Select(bs => new { bs.BookedDate.Date, bs.SlotIndex })
                .ToListAsync();

            foreach (var offeredSlot in offer.OfferedSlots)
            {
                var slotDate = offeredSlot.SlotDateTime.Date;
                var slotIndex = offeredSlot.SlotIndex;

                if (existingBookedSlots.Any(bs => bs.Date == slotDate && bs.SlotIndex == slotIndex))
                {
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        ErrorCode.BadRequest,
                        $"You already have a booking for slot {slotIndex} on {slotDate:dd/MM/yyyy}. Please check your schedule.");
                }
            }

            var slotCount = offer.OfferedSlots.Count;
            var totalPrice = offer.Lesson.Price * slotCount;

            var learnerWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == learnerId);

            if (learnerWallet == null)
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Learner wallet not found");

            if (learnerWallet.Balance < totalPrice)
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Insufficient funds in wallet");

            return (offer, learnerWallet, totalPrice);
        }

        private async Task<(
            Lesson lesson, 
            Tutor tutor, 
            Wallet learnerWallet, 
            List<(DateTime date, int slotIndex)> slots, 
            decimal totalPrice)> 
            ValidateInstantBookingConditionsAsync(InstantBookingRequest request, string learnerId)
        {
            // Validate tutor
            var tutor = await _unitOfWork.GetRepository<Tutor>()
                .ExistEntities()
                .FirstOrDefaultAsync(t => t.UserId == request.TutorId);
                
            if (tutor == null)
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Tutor not found");
                
            // Validate lesson
            var lesson = await _unitOfWork.GetRepository<Lesson>()
                .ExistEntities()
                .FirstOrDefaultAsync(l => l.Id == request.LessonId && l.TutorId == request.TutorId);
                
            if (lesson == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Lesson not found or does not belong to this tutor");
                    
            // Check booking config - ONLY FOR INSTANT BOOKING
            var bookingConfig = await _unitOfWork.GetRepository<BookingConfig>()
                .ExistEntities()
                .FirstOrDefaultAsync(bc => bc.TutorId == request.TutorId);
                
            if (bookingConfig == null || !bookingConfig.AllowInstantBooking)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "This tutor does not allow instant booking");
                    
            var now = DateTime.UtcNow;
            var minAllowedTime = now.AddHours(MIN_HOURS_BEFORE_BOOKING);
            var slots = new List<(DateTime date, int slotIndex)>();
            
            foreach (var slotRequest in request.Slots)
            {
                var slotDate = slotRequest.SlotDate.Date;
                var slotIndex = slotRequest.SlotIndex;
                
                var slotStartTime = CalculateSlotStartTime(slotDate, slotIndex);
                if (slotStartTime <= minAllowedTime)
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        ErrorCode.BadRequest,
                        $"Cannot book slots that start less than {MIN_HOURS_BEFORE_BOOKING} hour(s) from now");
                        
                slots.Add((slotDate, slotIndex));
            }
            
            if (bookingConfig != null)
            {
                var existingBookedSlotsCount = await _unitOfWork.GetRepository<BookedSlot>()
                    .ExistEntities()
                    .Include(bs => bs.Booking)
                    .CountAsync(bs => bs.Booking!.TutorId == request.TutorId && 
                            bs.Booking!.LearnerId == learnerId &&
                            (bs.Status == SlotStatus.Pending));

                if (existingBookedSlotsCount + slots.Count > bookingConfig.MaxInstantBookingSlots)
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        ErrorCode.BadRequest,
                        $"You cannot book more than {bookingConfig.MaxInstantBookingSlots} slots with this tutor");
            }
            
            var existingBookedSlots = await _unitOfWork.GetRepository<BookedSlot>()
                .ExistEntities()
                .Include(bs => bs.Booking)
                .Where(bs => bs.Booking!.LearnerId == learnerId && 
                        (bs.Status == SlotStatus.Pending))
                .Select(bs => new { bs.BookedDate.Date, bs.SlotIndex })
                .ToListAsync();

            foreach (var (slotDate, slotIndex) in slots)
            {
                if (existingBookedSlots.Any(bs => bs.Date == slotDate && bs.SlotIndex == slotIndex))
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        ErrorCode.BadRequest,
                        $"You already have a booking for slot {slotIndex} on {slotDate:dd/MM/yyyy}");
            }
            
            var totalPrice = lesson.Price * slots.Count;
            
            var learnerWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == learnerId);
                
            if (learnerWallet == null)
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Learner wallet not found");
                
            if (learnerWallet.Balance < totalPrice)
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "Insufficient funds in wallet");
                
            return (lesson, tutor, learnerWallet, slots, totalPrice);
        }

        private async Task<BookingResponse> CreateBookingAsync(
            string tutorId,
            string learnerId,
            Lesson lesson,
            List<(DateTime date, int slotIndex)> slots,
            Wallet learnerWallet,
            decimal totalPrice,
            TutorBookingOffer? offer = null)  
        {
            var lessonSnapshot = LessonSnapshot.CreateFromLesson(lesson);
            
            return await _unitOfWork.ExecuteInTransactionAsync(async () => {
                _unitOfWork.GetRepository<LessonSnapshot>().Insert(lessonSnapshot);
                
                var booking = new Booking
                {
                    TutorId = tutorId,
                    LearnerId = learnerId,
                    Note = offer != null 
                        ? $"Booking created from offer {offer.Id}" 
                        : "Instant booking",
                    LessonSnapshotId = lessonSnapshot.Id,
                    OriginalOfferId = offer?.Id
                };
                
                booking.TrackCreate(learnerId);
                _unitOfWork.GetRepository<Booking>().Insert(booking);
                
                var bookedSlots = new List<BookedSlot>();
                var heldFunds = new List<HeldFund>();
                var escrowWallet = await GetEscrowWalletAsync();
                
                foreach (var (slotDate, slotIndex) in slots)
                {
                    var slotStartTime = CalculateSlotStartTime(slotDate, slotIndex);
                    var slotEndTime = CalculateSlotEndTime(slotDate, slotIndex);
                    var releaseTime = slotEndTime.AddHours(RELEASE_TIME_OFFSET_HOURS); 
                    
                    var heldFund = HeldFund.CreateForBooking(string.Empty, lesson.Price, releaseTime);
                    _unitOfWork.GetRepository<HeldFund>().Insert(heldFund);
                    heldFunds.Add(heldFund);
                    
                    // Tạo booked slot
                    var bookedSlot = new BookedSlot
                    {
                        BookingId = booking.Id,
                        BookedDate = slotDate, 
                        SlotIndex = slotIndex,
                        Status = SlotStatus.Pending, 
                        HeldFundId = heldFund.Id
                    };
                    
                    bookedSlot.TrackCreate(learnerId);
                    _unitOfWork.GetRepository<BookedSlot>().Insert(bookedSlot);
                    bookedSlots.Add(bookedSlot);
                    
                    heldFund.BookedSlotId = bookedSlot.Id;
                    _unitOfWork.GetRepository<HeldFund>().UpdateFields(heldFund, h => h.BookedSlotId!); 

                    HangfireConfig.ScheduleSlotStatusUpdateJob(bookedSlot.Id, slotEndTime);
                }
                
                var transaction = Transaction.CreatePaymentTransaction(
                    learnerWallet.Id,
                    escrowWallet.Id,
                    totalPrice,
                    booking.Id,
                    $"Thanh toán cho booking: {booking.Id} với {slots.Count} slot được giữ trong ví ký quỹ"
                );

                _unitOfWork.GetRepository<Transaction>().Insert(transaction);
                
                var learnerUpdateFields = learnerWallet.SubtractBalance(totalPrice);
                _unitOfWork.GetRepository<Wallet>().UpdateFields(learnerWallet, learnerUpdateFields);
                
                var escrowUpdateFields = escrowWallet.AddBalance(totalPrice);
                _unitOfWork.GetRepository<Wallet>().UpdateFields(escrowWallet, escrowUpdateFields);
                
                if (offer != null)
                    _unitOfWork.GetRepository<TutorBookingOffer>().Delete(offer);
                
                await _unitOfWork.SaveAsync();
                
                foreach (var heldFund in heldFunds)
                {
                    if (heldFund.ReleaseAt.HasValue)
                    {
                        HangfireConfig.ScheduleHeldFundReleaseJob(heldFund.Id, heldFund.ReleaseAt.Value);
                    }
                }

                string notificationTitle = offer != null 
                    ? "Offer đã được chấp nhận" 
                    : "Bạn có một buổi dạy mới";

                string notificationContent = offer != null 
                    ? "Một học viên đã chấp nhận offer của bạn." 
                    : "Một học viên đã đặt lịch học với bạn";

                string notificationType = offer != null 
                    ? "OfferAccepted" 
                    : "InstantBookingReceived";

                await _notificationService.SendToUsersAsync(new()
                {
                    Content = new()
                    {
                        NotificationPriority = ENotificationPriority.Normal,
                        Title = notificationTitle,
                        Content = notificationContent,
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            Type = notificationType,
                            Id = booking.Id,
                            LessonName = lessonSnapshot.Name,
                            SenderId = learnerId,
                        }),
                    },
                    ReceiverUserIds = [booking.TutorId]
                });
                
                return BookingResponse.FromEntity(booking, lessonSnapshot, bookedSlots, totalPrice);
            });
        }

        public async Task<TutorBookingOfferResponse> RejectBookingOfferAsync(string offerId)
        {
            var learnerId = GetAuthenticatedLearnerId();
            
            var offerRepo = _unitOfWork.GetRepository<TutorBookingOffer>();
            var slotRepo = _unitOfWork.GetRepository<OfferedSlot>();
            
            var offer = await offerRepo.ExistEntities()
                .Include(o => o.OfferedSlots)
                .Include(o => o.Tutor).ThenInclude(t => t!.User)
                .Include(o => o.Learner).ThenInclude(l => l!.User)
                .Include(o => o.Lesson)
                .FirstOrDefaultAsync(o => o.Id == offerId && o.LearnerId == learnerId);

            if (offer == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Offer not found or you don't have permission to reject it.");

            if (offer.IsExpired)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Cannot reject an expired offer.");

            if (offer.OfferedSlots?.Any() == true)
                slotRepo.DeleteRange(offer.OfferedSlots);

            var updateFields = offer.MarkAsRejected();
            if (updateFields.Any())
                offerRepo.UpdateFields(offer, updateFields);
                
            await _unitOfWork.SaveAsync();
            
            #region Send notification to tutor
            // // Gửi thông báo cho tutor
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
            #endregion

            return await offerRepo.ExistEntities()
                .Where(o => o.Id == offerId)
                .Include(o => o.Tutor).ThenInclude(t => t!.User)
                .Include(o => o.Learner).ThenInclude(l => l!.User)
                .Include(o => o.Lesson)
                .Include(o => o.OfferedSlots)
                .Select(TutorBookingOfferResponse.Projection)
                .FirstAsync();
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

        private DateTime CalculateSlotStartTime(DateTime date, int slotIndex)
        {
            return date.Date.AddMinutes(slotIndex * 30);
        }

        private DateTime CalculateSlotEndTime(DateTime date, int slotIndex)
        {
            return date.Date.AddMinutes((slotIndex + 1) * 30);
        }


        public async Task<BookingResponse> AcceptTutorOfferAsync(AcceptOfferRequest request)
        {
            var learnerId = _currentUserProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(learnerId))
                throw new ErrorException(StatusCodes.Status401Unauthorized, ErrorCode.Unauthorized, "User not authenticated");

            var (offer, learnerWallet, totalPrice) = await ValidateOfferAndBookingConditionsAsync(request.OfferId, learnerId);

            var slots = offer.OfferedSlots.Select(os => (os.SlotDateTime.Date, os.SlotIndex)).ToList();

            return await CreateBookingAsync(
                offer.TutorId, 
                learnerId, 
                offer.Lesson!, 
                slots,
                learnerWallet, 
                totalPrice,
                offer); 
        }

        public async Task<BookingResponse> CreateInstantBookingAsync(InstantBookingRequest request)
        {
            var learnerId = _currentUserProvider.GetCurrentUserId();
            if (string.IsNullOrEmpty(learnerId))
                throw new ErrorException(StatusCodes.Status401Unauthorized, ErrorCode.Unauthorized, "User not authenticated");

            var (lesson, tutor, learnerWallet, slots, totalPrice) = 
                await ValidateInstantBookingConditionsAsync(request, learnerId);

            return await CreateBookingAsync(
                tutor.UserId, 
                learnerId, 
                lesson, 
                slots,
                learnerWallet, 
                totalPrice,
                null); 
        }
        public async Task<BookingResponse> CancelBookingAsync(string bookingId, string? cancellationReason = null)
        {
            var learnerId = GetAuthenticatedLearnerId();
            
            // Get booking with all related data
            var booking = await _unitOfWork.GetRepository<Booking>()
                .ExistEntities()
                .Include(b => b.BookedSlots!).ThenInclude(bs => bs.HeldFund)
                .Include(b => b.LessonSnapshot)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.LearnerId == learnerId);
                
            if (booking == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Booking not found or you don't have permission to cancel it.");
            
            // Determine booking type (Offer vs Instant)
            bool isOfferBooking = !string.IsNullOrEmpty(booking.OriginalOfferId);
            
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
            
            var learnerWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == learnerId);
            
            if (learnerWallet == null)
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    ErrorCode.ServerError,
                    "Learner wallet not found");
            
            var escrowWallet = await GetEscrowWalletAsync();
            var tutorWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.UserId == booking.TutorId);
                
            if (tutorWallet == null)
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    ErrorCode.ServerError,
                    "Tutor wallet not found");
            
            // Get completed slots to determine trial period for Offer Booking
            var completedSlotsCount = booking.BookedSlots!
                .Count(bs => bs.Status == SlotStatus.Completed || bs.Status == SlotStatus.AwaitingPayout);
            
            // Get first slot to check 24-hour policy
            var firstPendingSlot = slotsToCancel.OrderBy(s => s.BookedDate).ThenBy(s => s.SlotIndex).FirstOrDefault();
            var now = DateTime.UtcNow;
            bool isWithin24Hours = false;
            
            if (firstPendingSlot != null)
            {
                var firstSlotStartTime = CalculateSlotStartTime(firstPendingSlot.BookedDate, firstPendingSlot.SlotIndex);
                isWithin24Hours = (firstSlotStartTime - now).TotalHours <= 24;
            }
            
            return await _unitOfWork.ExecuteInTransactionAsync(async () => {
                foreach (var slot in slotsToCancel)
                {
                    // Update slot status
                    var updateFields = slot.MarkAsCancelled(learnerId);
                    _unitOfWork.GetRepository<BookedSlot>().UpdateFields(slot, updateFields);
                    
                    // Process held funds
                    if (slot.HeldFund != null)
                    {
                        decimal tutorPercentage = 0;
                        decimal learnerPercentage = 1.0m;
                        
                        if (isOfferBooking)
                        {
                            if (completedSlotsCount == 0 && !isWithin24Hours)
                            {
                                // Offer Booking cancelled 24+ hours before first slot: 100% refund to learner
                                tutorPercentage = 0;
                                learnerPercentage = 1.0m;
                            }
                            else if (completedSlotsCount == 0 && isWithin24Hours)
                            {
                                // Offer Booking cancelled within 24 hours before first slot: 97% refund to learner
                                tutorPercentage = 0.03m;
                                learnerPercentage = 0.97m;
                            }
                            else if (completedSlotsCount > 0 && completedSlotsCount < 3)
                            {
                                // Offer Booking trial period (slots 1-3): 90% refund for remaining slots
                                tutorPercentage = 0.1m;
                                learnerPercentage = 0.9m;
                            }
                            else
                            {
                                // Offer Booking after trial (slot 4+): no refund
                                tutorPercentage = 1.0m;
                                learnerPercentage = 0;
                            }
                        }
                        else // Instant Booking
                        {
                            if (!isWithin24Hours && completedSlotsCount == 0)
                            {
                                // Instant Booking cancelled 24+ hours before first slot: 10% refund to learner
                                tutorPercentage = 0.1m;
                                learnerPercentage = 0.9m;
                            }
                            else
                            {
                                // Instant Booking cancelled within 24 hours or after first slot: no refund
                                tutorPercentage = 1.0m;
                                learnerPercentage = 0;
                            }
                        }
                        
                        decimal tutorAmount = slot.HeldFund.Amount * tutorPercentage;
                        decimal learnerAmount = slot.HeldFund.Amount * learnerPercentage;
                        
                        if (tutorAmount > 0)
                        {
                            // Transfer funds to tutor
                            var tutorTransaction = Transaction.CreatePaymentTransaction(
                                escrowWallet.Id,
                                tutorWallet.Id,
                                tutorAmount,
                                slot.Id,
                                $"Thanh toán cho gia sư do huỷ slot {slot.Id}"
                            );
                            _unitOfWork.GetRepository<Transaction>().Insert(tutorTransaction);
                            
                            var tutorWalletUpdateFields = tutorWallet.AddBalance(tutorAmount);
                            _unitOfWork.GetRepository<Wallet>().UpdateFields(tutorWallet, tutorWalletUpdateFields);
                            
                            var escrowUpdateFields = escrowWallet.SubtractBalance(tutorAmount);
                            _unitOfWork.GetRepository<Wallet>().UpdateFields(escrowWallet, escrowUpdateFields);
                        }
                        
                        if (learnerAmount > 0)
                        {
                            // Refund to learner
                            var learnerTransaction = Transaction.CreatePaymentTransaction(
                                escrowWallet.Id,
                                learnerWallet.Id,
                                learnerAmount,
                                slot.Id,
                                $"Hoàn tiền cho học viên do huỷ slot {slot.Id}"
                            );
                            _unitOfWork.GetRepository<Transaction>().Insert(learnerTransaction);
                            
                            var learnerWalletUpdateFields = learnerWallet.AddBalance(learnerAmount);
                            _unitOfWork.GetRepository<Wallet>().UpdateFields(learnerWallet, learnerWalletUpdateFields);
                            
                            var escrowUpdateFields2 = escrowWallet.SubtractBalance(learnerAmount);
                            _unitOfWork.GetRepository<Wallet>().UpdateFields(escrowWallet, escrowUpdateFields2);
                        }
                        
                        // Update held fund status
                        HeldFundStatus newStatus = learnerAmount > 0 ? 
                            (tutorAmount > 0 ? HeldFundStatus.Disputed : HeldFundStatus.RefundedToLearner) : 
                            HeldFundStatus.ReleasedToTutor;
                        
                        var heldFundUpdateFields = slot.HeldFund.UpdateStatus(newStatus);
                        _unitOfWork.GetRepository<HeldFund>().UpdateFields(slot.HeldFund, heldFundUpdateFields);
                    }
                }
                
                // Update booking status if all slots are cancelled
                if (!booking.BookedSlots!.Any(bs => bs.Status != SlotStatus.Cancelled && bs.Status != SlotStatus.CancelledDisputed))
                {
                    var bookingUpdateFields = booking.UpdateStatus(BookingStatus.Cancelled, learnerId);
                    _unitOfWork.GetRepository<Booking>().UpdateFields(booking, bookingUpdateFields);
                }
                
                await _unitOfWork.SaveAsync();
                
                // Send notification to tutor
                await _notificationService.SendToUsersAsync(new()
                {
                    Content = new()
                    {
                        NotificationPriority = ENotificationPriority.Normal,
                        Title = "Đặt lịch đã bị huỷ",
                        Content = "Một học viên đã huỷ đặt lịch với bạn.",
                        AdditionalData = JsonSerializer.Serialize(new
                        {
                            Type = "BookingCancelled",
                            BookingId = booking.Id,
                            LessonName = booking.LessonSnapshot?.Name,
                            SenderId = learnerId,
                            CancellationReason = cancellationReason ?? "No reason provided"
                        })
                    },
                    ReceiverUserIds = [booking.TutorId]
                });
                
                return BookingResponse.FromEntity(
                    booking, 
                    booking.LessonSnapshot!, 
                    booking.BookedSlots!.ToList(),
                    booking.BookedSlots!.Sum(bs => bs.HeldFund?.Amount ?? 0)
                );
            });
        }
    }
}
