using App.DTOs.UserDTOs;
using App.Repositories.Models;

namespace App.Services.Interfaces.User
{
    public interface IUserService
    {
        Task<UserProfileResponse> GetUserProfileAsync(string userId);
        Task UpdateUserProfileAsync(string userId, UpdateUserRequest model);
        Task ChangePasswordAsync(string userId, ChangePasswordRequest model);

        string GetCurrentUserId();
        Task<AppUser> GetCurrentUserAsync();
        Task<AppUser> GetUserByIdAsync(string userId);
        Task<AppUser> GetUserWithValidMatchingId(string userId, string? errorMessage = null);

        Task AssignRoleAsync(string userId, string roleName);
    }
}
        //Task<bool> IsUserRegisteredAsTutorAsync(string userId);
