using App.DTOs.UserDTOs;
using App.Repositories.Models.User;
using Microsoft.AspNetCore.Identity;

namespace App.Services.Interfaces.User
{
    public interface IUserService
    {
        Task<List<IdentityRole>> GetAllRolesAsync();
        Task AddRoleToUserAsync(string userId, string roleName);
        Task RemoveRoleFromUserAsync(string userId, string roleName);
        Task<IList<string>> GetUserRolesAsync(string userId);
        
        Task<UserProfileResponse> GetUserProfileAsync(string userId);
        Task UpdateUserProfileAsync(string userId, UpdateUserRequest model);
        Task ChangePasswordAsync(string userId, ChangePasswordRequest model);

        string GetCurrentUserId();
        Task<List<AppUser>> GetAllUsersAsync();
        Task<AppUser> GetCurrentUserAsync();
        Task<AppUser> GetUserByIdAsync(string userId);
        Task<AppUser> GetUserWithValidMatchingId(string userId, string? errorMessage = null);

        Task AssignRoleAsync(string userId, string roleName);

    }
}
        //Task<bool> IsUserRegisteredAsTutorAsync(string userId);
