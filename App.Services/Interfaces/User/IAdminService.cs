using App.Core.Base;
using App.DTOs.AppUserDTOs.AdminDTOs;

namespace App.Services.Interfaces.User
{
    public interface IAdminService
    {
        Task<BasePaginatedList<StaffManagerResponse>> GetAllManagersAsync(StaffManagerFilterRequest filter);
        Task<BasePaginatedList<StaffManagerResponse>> GetAllStaffsAsync(StaffManagerFilterRequest filter);
        Task<StaffManagerResponse> CreateStaffManagerAsync(CreateStaffManagerRequest request);
        Task<BasePaginatedList<UserResponse>> GetAllUsersAsync(UserFilterRequest filter);
        Task<StaffManagerResponse> ToggleAccountStatusAsync(AccountStatusRequest request);
        Task DeleteAccountAsync(string userId);
        Task<StaffManagerResponse> ChangePasswordAsync(ChangePasswordRequest request);
    }
}