using App.DTOs.UserDTOs;

namespace App.Services.Interfaces.User
{
    public interface IUserService
    {
        Task<UserProfileResponse> GetUserProfileAsync(string userId);
        Task UpdateUserProfileAsync(string userId, UpdateUserRequest model);
        Task ChangePasswordAsync(string userId, ChangePasswordRequest model);
    }
}
