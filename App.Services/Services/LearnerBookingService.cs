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

            var repo = _unitOfWork.GetRepository<LearnerTimeSlotRequest>();
            
            // Delete all existing requests for this learner-tutor pair
            var existingRequests = await repo.ExistEntities()
                .Where(r => r.LearnerId == learnerId && r.TutorId == request.TutorId)
                .ToListAsync();

            if (existingRequests.Any())
            {
                repo.DeleteRange(existingRequests);
                await _unitOfWork.SaveAsync();
            }

            // Insert all new requests from the DTO
            var newRequests = request.ToEntities(learnerId);
            if (newRequests.Any())
            {
                repo.InsertRange(newRequests);
                await _unitOfWork.SaveAsync();
            }
        }

        public async Task DeleteTimeSlotRequestsAsync(string tutorId)
        {
            var learnerId = GetAuthenticatedLearnerId();
            await ValidateTutorExistsAsync(tutorId);

            var repo = _unitOfWork.GetRepository<LearnerTimeSlotRequest>();
            var requestsToDelete = await repo.ExistEntities()
                .Where(r => r.LearnerId == learnerId && r.TutorId == tutorId)
                .ToListAsync();

            if (requestsToDelete.Any())
            {
                repo.DeleteRange(requestsToDelete);
                await _unitOfWork.SaveAsync();
            }
        }

        public async Task<List<LearnerTimeSlotResponseDTO>> GetTimeSlotRequestsByTutorAsync(string tutorId)
        {
            var learnerId = GetAuthenticatedLearnerId();
            await ValidateTutorExistsAsync(tutorId);

            return await _unitOfWork.GetRepository<LearnerTimeSlotRequest>()
                .ExistEntities()
                .Where(r => r.LearnerId == learnerId && r.TutorId == tutorId)
                .Select(LearnerTimeSlotResponseDTO.Projection)
                .ToListAsync();
        }

        // public async Task<List<LearnerTimeSlotResponseDTO>> GetTimeSlotRequestsByLearnerAsync(string learnerId)
        // {
        //     var tutorId = GetAuthenticatedTutorId();
        //     var repo = _unitOfWork.GetRepository<LearnerTimeSlotRequest>();
            
        //     // Get requests
        //     var requests = await repo.ExistEntities()
        //         .Where(r => r.LearnerId == learnerId && r.TutorId == tutorId)
        //         .ToListAsync();

        //     // Mark as viewed
        //     foreach (var request in requests)
        //     {
        //         var updateFields = request.MarkAsViewed();
        //         repo.UpdateFields(request, updateFields);
        //     }
        //     await _unitOfWork.SaveAsync();

        //     return requests.Select(LearnerTimeSlotResponseDTO.Projection.Compile()).ToList();
        // }

        // public async Task<LearnerTimeSlotResponseDTO> GetTimeSlotRequestByIdAsync(string requestId)
        // {
        //     var tutorId = GetAuthenticatedTutorId();

        //     var request = await _unitOfWork.GetRepository<LearnerTimeSlotRequest>()
        //         .ExistEntities()
        //         .Where(r => r.Id == requestId && r.TutorId == tutorId)
        //         .Select(LearnerTimeSlotResponseDTO.Projection)
        //         .FirstOrDefaultAsync();

        //     if (request == null)
        //         throw new ErrorException(
        //             StatusCodes.Status404NotFound,
        //             ErrorCode.NotFound,
        //             "Time slot request not found or you don't have permission to view it.");

        //     return request;
        // }

        // public async Task<List<LearnerInfoDTO>> GetAllTimeSlotRequestsForTutorAsync()
        // {
        //     var tutorId = GetAuthenticatedTutorId();

        //     return await _unitOfWork.GetRepository<LearnerTimeSlotRequest>()
        //         .ExistEntities()
        //         .Include(r => r.Learner)
        //         .ThenInclude(l => l.User)
        //         .Where(r => r.TutorId == tutorId)
        //         .GroupBy(r => new { r.LearnerId, r.Learner.User.FullName })
        //         .Select(g => new LearnerInfoDTO
        //         {
        //             LearnerId = g.Key.LearnerId,
        //             LearnerName = g.Key.FullName,
        //             HasUnviewed = g.Any(r => !r.LastViewedAt.HasValue),
        //             LatestRequestTime = g.Max(r => r.CreatedAt)
        //         })
        //         .OrderByDescending(x => x.LatestRequestTime)
        //         .ToListAsync();
        // }
    }
}
