using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.BookingDTOs;
using App.Repositories.Models;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using App.Repositories.Models.Booking;
using App.DTOs.UserDTOs;
using App.Repositories.Models.User;
using System.Linq.Expressions;
using System.Text.Json;

namespace App.Services.Services
{
    public class TutorBookingService : ITutorBookingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;

        public TutorBookingService(
            IUnitOfWork unitOfWork,
            ICurrentUserProvider currentUserProvider)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
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

            var newOffer = new TutorBookingOffer
            {
                TutorId = tutorId,
                LearnerId = request.LearnerId,
                LessonId = request.LessonId,
                TotalPrice = lesson.Price * request.OfferedSlots.Count,
                OfferedSlots = request.OfferedSlots.Select(s => new OfferedSlot
                {
                    SlotDateTime = s.SlotDateTime,
                    SlotIndex = s.SlotIndex,
                }).ToList()
            };

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
            offer.TotalPrice = offer.Lesson.Price * offer.OfferedSlots.Count;
            offer.UpdatedAt = DateTime.UtcNow;
            
            offerRepo.UpdateFields(offer, o => o.TotalPrice, o => o.UpdatedAt!);
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

            offerRepo.Delete(offer);
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
    }
}