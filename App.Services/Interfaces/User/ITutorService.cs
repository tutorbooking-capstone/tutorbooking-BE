using App.DTOs.AppUserDTOs.TutorDTOs;
using App.DTOs.HashtagDTOs;
using App.Repositories.Models.User;

namespace App.Services.Interfaces.User
{
    public interface ITutorService
    {
        // Tutor Registration
        Task<TutorResponse> RegisterAsTutorAsync(TutorRegistrationRequest request);

        // Profile Updates 
        Task UpdateLanguagesAsync(List<TutorLanguageDTO> languages);
        Task UpdateHashtagsAsync(UpdateTutorHashtagListRequest request);

        // Retrieval
        Task<TutorResponse> GetByIdAsync(string tutorId);
        Task<VerificationStatus> GetVerificationStatusAsync(string tutorId);

        // Status Management 
        Task UpdateVerificationStatusAsync(string id, VerificationStatus status, string? updatedBy = null);
    }
}
