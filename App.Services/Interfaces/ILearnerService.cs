using App.DTOs.AppUserDTOs.LearnerDTOs;

namespace App.Services.Interfaces
{
    public interface ILearnerService
    {
        Task UpdateLearningLanguageAsync(UpdateLearnerLanguageRequest request);
        Task<(string LanguageCode, int ProficiencyLevel)> GetLearningLanguageAsync();
    }
}
