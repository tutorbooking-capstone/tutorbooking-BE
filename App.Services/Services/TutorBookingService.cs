using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.BookingDTOs;
using App.Repositories.Models;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

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

        public async Task<List<LearnerInfoDTO>> GetAllTimeSlotRequestsForTutorAsync()
        {
            var tutorId = GetAuthenticatedTutorId();

            return await _unitOfWork.GetRepository<LearnerTimeSlotRequest>()
                .ExistEntities()
                .Include(r => r.Learner)
                .ThenInclude(l => l.User)
                .Where(r => r.TutorId == tutorId)
                .GroupBy(r => new { r.LearnerId, FullName = r.Learner!.User!.FullName ?? "" })
                .Select(g => new LearnerInfoDTO
                {
                    LearnerId = g.Key.LearnerId,
                    LearnerName = g.Key.FullName,
                    HasUnviewed = g.Any(r => !r.LastViewedAt.HasValue),
                    LatestRequestTime = g.Max(r => r.CreatedAt)
                })
                .OrderByDescending(x => x.LatestRequestTime)
                .ToListAsync();
        }

        public async Task<List<LearnerTimeSlotResponseDTO>> GetTimeSlotRequestsByLearnerAsync(string learnerId)
        {
            var tutorId = GetAuthenticatedTutorId();
            var repo = _unitOfWork.GetRepository<LearnerTimeSlotRequest>();
            
            // Get requests
            var requests = await repo.ExistEntities()
                .Where(r => r.LearnerId == learnerId && r.TutorId == tutorId)
                .ToListAsync();

            // Mark as viewed
            foreach (var request in requests)
            {
                var updateFields = request.MarkAsViewed();
                repo.UpdateFields(request, updateFields);
            }
            await _unitOfWork.SaveAsync();

            return requests.Select(LearnerTimeSlotResponseDTO.Projection.Compile()).ToList();
        }
    }
}