using App.Core.Base;
using App.Core.Constants;
using App.DTOs.UserDTOs;
using App.Repositories.Models.User;
using App.Services.Infras;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace App.Services.Services.User
{
    public class UserService : IUserService
    {
        #region DI Constructor
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHttpContextAccessor _contextAccessor;

        public UserService(
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IHttpContextAccessor contextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _contextAccessor = contextAccessor;
        }
        #endregion

        // public async Task<UserProfileResponse> GetUserProfileAsync(string userId)
        // {
        //     var user = await GetUserByIdAsync(userId);
        //     return user.ToUserProfileResponse();
        // }

        public async Task UpdateUserProfileAsync(string userId, UpdateUserRequest model)
        {
            var user = await GetUserByIdAsync(userId);
            model.ApplyTo(user);
            
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ResponseCodeConstants.BADREQUEST, 
                    "Không thể cập nhật thông tin người dùng");
        }

        public async Task ChangePasswordAsync(string userId, ChangePasswordRequest model)
        {
            var user = await GetUserByIdAsync(userId);

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ResponseCodeConstants.BADREQUEST, 
                    "Mật khẩu hiện tại không đúng hoặc mật khẩu mới không hợp lệ.");
        }

        public string GetCurrentUserId()
        {
            var user = _contextAccessor.HttpContext?.User;
            
            if (user != null)
            {
                var claims = user.Claims.Select(c => $"{c.Type}: {c.Value}");
                //_logger.LogDebug("User claims: {ClaimsString}", string.Join(", \n", claims));
            }

            var userId = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return userId ?? throw new UnauthorizedException("Cannot get userId from NameIdentifier claim");
        }

        public bool IsInRole(string roleName)
        {
            var user = _contextAccessor.HttpContext?.User;
            return user?.IsInRole(roleName) ?? false;
        }

        public async Task<AppUser> GetCurrentUserAsync()
            => await GetUserByIdAsync(GetCurrentUserId());

        public async Task<AppUser> GetUserByIdAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.DeletedTime.HasValue)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ResponseCodeConstants.NOT_FOUND,
                    "Không tìm thấy người dùng");

            return user;
        }

        public async Task<AppUser> GetUserWithValidMatchingId(
            string userId, 
            string? authorizationErrorMessage = null)
        {
            string currentUserId = GetCurrentUserId();
            if (currentUserId != userId)
                throw new ErrorException(
                    StatusCodes.Status403Forbidden,
                    ErrorCode.Forbidden,
                    authorizationErrorMessage ?? "Bạn không có quyền thực hiện hành động này trên tài nguyên của người dùng khác.");

            return await GetUserByIdAsync(userId);
        }

        public async Task AssignRoleAsync(string userId, string roleName)
        {
            var user = await GetUserByIdAsync(userId);
            
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Không thể gán vai trò cho người dùng");
        }

        public async Task<List<AppUser>> GetAllUsersAsync()
            => await _userManager.Users.ToListAsync();

        public async Task<List<IdentityRole>> GetAllRolesAsync()
            => await _roleManager.Roles.ToListAsync();

        public async Task AddRoleToUserAsync(string userId, string roleName)
        {
            var user = await GetUserByIdAsync(userId);
            
            // BAD PERFORMANCE!!!
            // if (await _userManager.IsInRoleAsync(user, roleName))
            //     return;
        
            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Không thể thêm vai trò cho người dùng");
        }

        public async Task RemoveRoleFromUserAsync(string userId, string roleName)
        {
            var user = await GetUserByIdAsync(userId);
            
            // BAD PERFORMANCE!!!
            // if (!await _userManager.IsInRoleAsync(user, roleName))
            //     return;
        
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Không thể xóa vai trò của người dùng");
        }

        public async Task<IList<string>> GetUserRolesAsync(string userId)
        {
            var user = await GetUserByIdAsync(userId);
            return await _userManager.GetRolesAsync(user);
        }
    }
}


//public async Task<bool> IsUserRegisteredAsTutorAsync(string userId)
//{
//    var user = await _userManager.FindByIdAsync(userId);
//    return user != null && !user.DeletedTime.HasValue;
//}