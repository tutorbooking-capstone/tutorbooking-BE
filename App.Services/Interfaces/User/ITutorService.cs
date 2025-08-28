using App.Core.Base;
using App.DTOs.AppUserDTOs.TutorDTOs;
using App.DTOs.HashtagDTOs;
using App.DTOs.UserDTOs;
using App.Repositories.Models.Scheduling;

namespace App.Services.Interfaces.User
{
    public interface ITutorService
    {
        // Tutor Registration
        Task<TutorResponse> RegisterAsTutorAsync(TutorRegistrationRequest request);
        Task<TutorResponse> SeedRegisterAsTutorAsync(string userId, TutorRegistrationRequest request);

        // Profile Updates 
        Task UpdateLanguagesAsync(List<TutorLanguageDTO> languages);
        Task UpdateTutorHashtagsAsync(UpdateTutorHashtagListRequest request);

        // Retrieval
        Task<TutorResponse> GetByIdAsync(string tutorId);
        Task<bool> GetVerificationStatusAsync(string tutorId);
        Task<List<TutorHashtagDTO>> GetTutorHashtagsAsync();
        Task<List<TutorLanguageDTO>> GetTutorLanguagesAsync();
        Task<List<TutorCardDTO>> GetTutorCardListAsync();
		Task<BasePaginatedList<TutorCardDTO>> GetTutorCardsPagingAsync(string[]? languageCodes,
            string? primaryLanguageCode,
            DayInWeek[]? daysInWeek,
            int[]? slotIndexes,
            decimal? minPrice,
            decimal? maxPrice,
            string[]? hashtags,
            string? tutorName,
            int page = 1,
            int size = 20);
        Task<Dictionary<string, List<TutorCardDTO>>> GetRecommendedTutorCardsAsync();

        Task<BookingConfigDTO> GetBookingConfigAsync(string tutorId);
        Task UpdateBookingConfigAsync(UpdateBookingConfigRequest request);
        Task SyncBookingConfigsAsync();
        Task UpdateTutorProfileAsync(UpdateTutorProfileRequest request);
        // Status Management 
        //Task UpdateVerificationStatusAsync(string id, VerificationStatus status, string? updatedBy = null);
    }
}