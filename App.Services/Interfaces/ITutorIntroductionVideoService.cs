using App.DTOs.AppUserDTOs.TutorDTOs;

namespace App.Services.Interfaces
{
    public interface ITutorIntroductionVideoService
    {
        Task<TutorIntroductionVideoResponse> CreateAsync(TutorIntroductionVideoRequest request);
        Task DeleteAsync(string id);
        Task<ICollection<TutorIntroductionVideoResponse>> GetByCurrentUserIdAsync(int page = 1, int size = 10);
        Task<TutorIntroductionVideoResponse?> GetByIdAsync(string id);
        Task<ICollection<TutorIntroductionVideoResponse>> GetPendingAsync(int page, int size);
        Task<TutorIntroductionVideoResponse> ReviewAsync(TutorIntroductionVideoReviewRequest request);
    }
}