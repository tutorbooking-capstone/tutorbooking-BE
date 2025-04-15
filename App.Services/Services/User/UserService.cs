using App.Core.Base;
using App.Core.Constants;
using App.DTOs.UserDTOs;
using App.Repositories.Models;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace App.Services.Services.User
{
    public class UserService : IUserService
    {
        #region DI Constructor
        private readonly UserManager<AppUser> _userManager;

        public UserService(UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }
        #endregion

        public async Task<UserProfileResponse> GetUserProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.DeletedTime.HasValue)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ResponseCodeConstants.NOT_FOUND, 
                    "Không tìm thấy người dùng");

            return user.ToUserProfileResponse();
        }

        public async Task UpdateUserProfileAsync(string userId, UpdateUserRequest model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || user.DeletedTime.HasValue)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ResponseCodeConstants.NOT_FOUND, 
                    "Không tìm thấy người dùng");

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
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.DeletedTime.HasValue)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ResponseCodeConstants.NOT_FOUND, 
                    "Không tìm thấy người dùng");

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ResponseCodeConstants.BADREQUEST, 
                    ("Mật khẩu hiện tại không đúng hoặc mật khẩu mới không hợp lệ."));
        }
    }

}
