using App.DTOs.AppUserDTOs.TutorDTOs;
using App.DTOs.UserDTOs;
using App.Repositories.Models.User;
using Microsoft.AspNetCore.Http;

namespace App.Services.Interfaces.User
{
    public interface IProfileService
    {
        Task<ProfileImageResponseDTO> UploadProfileImageAsync(IFormFile file);
        Task DeleteProfileImageAsync();
        Task UpdateFullNameAsync(string fullName);
        Task UpdateDateOfBirthAsync(DateTime? dateOfBirth);
        Task UpdateGenderAsync(Gender gender);
        Task UpdateBasicInformationAsync(UpdateBasicInformationRequest request);
        Task<UserProfileResponse> GetUserProfileAsync();
        Task<TutorRegistrationProfileResponse> GetTutorRegistrationProfileAsync();
        Task UpdateTutorInfoAsync(UpdateTutorInfoDTO request);
    }
}