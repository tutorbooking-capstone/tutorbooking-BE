using App.DTOs.UserDTOs;
using Microsoft.AspNetCore.Http;

namespace App.Services.Interfaces.User
{
    public interface IProfileService
    {
        Task<ProfileImageResponseDTO> UploadProfileImageAsync(IFormFile file);
        Task DeleteProfileImageAsync();
    }
}