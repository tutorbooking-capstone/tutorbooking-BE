using App.Core.Base;
using App.DTOs.AppUserDTOs.TutorDTOs;
using App.Repositories.Models;

namespace App.Services.Interfaces
{
    public interface ITutorIntroductionVideoService
    {
        #region Tutor
        Task<TutorIntroductionVideoResponse> CreateAsync(TutorIntroductionVideoRequest request);
        Task UpdateStatusAsync(TutorIntroductionVideoStatusUpdateRequest request);
        Task DeleteAsync(string id);
        #endregion

        #region Staff
        Task<TutorIntroductionVideoResponse> ReviewAsync(TutorIntroductionVideoReviewRequest request);
        #endregion

        Task<BasePaginatedList<TutorIntroductionVideoResponse>> GetByCurrentUserIdAsync(TutorIntroductionVideoStatus? status, int page = 1, int size = 10);
        Task<TutorIntroductionVideoResponse?> GetByIdAsync(string id);
        Task<BasePaginatedList<TutorIntroductionVideoResponse>> GetAsync(TutorIntroductionVideoStatus? status, string? userId, int page = 1, int size = 10);
       
    }
}