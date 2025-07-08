using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.BookingDTOs;
using App.Repositories.Models;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using App.Repositories.Models.Booking;
using System.Text.Json;

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

            if (existingRequest != null)
                repo.Delete(existingRequest);

            if (request.TimeSlots.Any())
            {
                var newRequest = request.ToEntity(learnerId);
                repo.Insert(newRequest);
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
    }
}
