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

            return await GetBookingOfferByIdForTutorAsync(offerId);
        }
        public async Task DeleteBookingOfferAsync(string offerId)
        {
            var tutorId = GetAuthenticatedTutorId();
            var offerRepo = _unitOfWork.GetRepository<TutorBookingOffer>();
            var offer = await offerRepo.ExistEntities().FirstOrDefaultAsync(o => o.Id == offerId && o.TutorId == tutorId);

            if (offer == null)
                throw new ErrorException(StatusCodes.Status404NotFound, ErrorCode.NotFound, "Offer not found or you don't have permission to delete it.");

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

            try
            {
                // Call the entity's behavior method to handle state transition and get updated fields
                var updateFields = bookedSlot.MarkAsCompleted(tutorId);

                // Only save if there are actual changes
                if (updateFields.Any())
                {
                    bookedSlotRepo.UpdateFields(bookedSlot, updateFields);
                    await _unitOfWork.SaveAsync();
                    // Fire Notification event after successful update

                    await _notificationService.SendToUsersAsync(new SendNotificationToUsersRequest()
                    {
                        Content = new()
                        {
                            NotificationPriority = ENotificationPriority.Normal,
                            Title = "PUSH_ON_BOOKED SLOT COMPLETED",
                            Content = "PUSH_ON_BOOKED_SLOT COMPLETED_BODY",
                            AdditionalData = JsonSerializer.Serialize(new
                            {
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
    }
}