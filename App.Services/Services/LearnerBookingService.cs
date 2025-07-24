using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.BookingDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services
{
    public class LearnerBookingService : ILearnerBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;

        public LearnerBookingService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
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
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Offer not found or not intended for this learner");

            if (offer.Lesson == null)
                throw new ErrorException(StatusCodes.Status400BadRequest, ErrorCode.BadRequest, "The lesson associated with this offer no longer exists");

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
                var systemWallet = await GetSystemWalletAsync();
                
                foreach (var offeredSlot in offer.OfferedSlots)
                {
                    // Calculate release time (24 hours after slot end time)
                    var slotEndTime = offeredSlot.SlotDateTime.AddMinutes(offer.Lesson.DurationInMinutes);
                    var releaseTime = slotEndTime.AddHours(24); // This should be configurable
                    
                    // Create held fund
                    var heldFund = HeldFund.Create(string.Empty, offer.Lesson.Price, releaseTime);
                    //heldFund.TrackCreate(learnerId);
                    _unitOfWork.GetRepository<HeldFund>().Insert(heldFund);
                    heldFunds.Add(heldFund);
                    
                    // Create booked slot
                    var bookedSlot = new BookedSlot
                    {
                        BookingId = booking.Id,
                        BookedDate = offeredSlot.SlotDateTime,
                        SlotIndex = offeredSlot.SlotIndex,
                        Status = SlotStatus.AwaitingConfirmation,
                        HeldFundId = heldFund.Id
                    };
                    
                    bookedSlot.TrackCreate(learnerId);
                    _unitOfWork.GetRepository<BookedSlot>().Insert(bookedSlot);
                    bookedSlots.Add(bookedSlot);
                    
                    // Update held fund with booked slot ID (circular reference)
                    heldFund.BookedSlotId = bookedSlot.Id;
                    _unitOfWork.GetRepository<HeldFund>().UpdateFields(heldFund, h => h.BookedSlotId);
                }
                
                // Create transaction to move funds from learner wallet to system wallet (escrow)
                var transaction = Transaction.CreatePaymentTransaction(
                    learnerWallet.Id,
                    totalPrice,
                    booking.Id,
                    $"Payment for booking {booking.Id} with {slotCount} slots"
                );
                
                _unitOfWork.GetRepository<Transaction>().Insert(transaction);
                
                // Update wallet balances
                var learnerUpdateFields = learnerWallet.UpdateBalance(learnerWallet.Balance - totalPrice);
                _unitOfWork.GetRepository<Wallet>().UpdateFields(learnerWallet, learnerUpdateFields);
                
                var systemUpdateFields = systemWallet.UpdateBalance(systemWallet.Balance + totalPrice);
                _unitOfWork.GetRepository<Wallet>().UpdateFields(systemWallet, systemUpdateFields);
                
                await _unitOfWork.SaveAsync();
                
                // Schedule release of funds using Hangfire
                foreach (var heldFund in heldFunds)
                {
                    BackgroundJob.Schedule<IPaymentProcessingService>(
                        service => service.ProcessHeldFundReleaseAsync(heldFund.Id),
                        heldFund.ReleaseAt - DateTime.UtcNow
                    );
                }
                
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

        private async Task<Wallet> GetSystemWalletAsync()
        {
            var systemWallet = await _unitOfWork.GetRepository<Wallet>()
                .ExistEntities()
                .FirstOrDefaultAsync(w => w.Type == WalletType.System);
                
            if (systemWallet == null)
                throw new ErrorException(StatusCodes.Status500InternalServerError, ErrorCode.ServerError, "System wallet not found");
                
            return systemWallet;
        }
    }
}
