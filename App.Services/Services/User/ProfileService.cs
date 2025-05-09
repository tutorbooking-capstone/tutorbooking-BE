using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.UserDTOs;
using App.Repositories.Models.User;
using App.Repositories.UoW; // For IUnitOfWork
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging; // For logging
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;


namespace App.Services.Services.User
{
    public class ProfileService : IProfileService
    {
        #region DI Constructor
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryProvider _cloudinaryProvider;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(
            IUserService userService,
            IUnitOfWork unitOfWork,
            ICloudinaryProvider cloudinaryProvider,
            ILogger<ProfileService> logger)
        {
            _userService = userService;
            _unitOfWork = unitOfWork;
            _cloudinaryProvider = cloudinaryProvider;
            _logger = logger;
        }
        #endregion

        public async Task<ProfileImageResponseDTO> UploadProfileImageAsync(IFormFile file)
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);

            if (!string.IsNullOrEmpty(user.ProfilePicturePublicId))
            {
                try
                {
                    bool deleted = await _cloudinaryProvider.DeleteImageAsync(user.ProfilePicturePublicId);
                    if (!deleted)
                    {
                        _logger.LogWarning("Failed to delete old profile image with PublicId {PublicId} from Cloudinary for user {UserId}.", user.ProfilePicturePublicId, userId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting old profile image with PublicId {PublicId} from Cloudinary for user {UserId}.", user.ProfilePicturePublicId, userId);
                }
            }

            var (newUrl, newPublicId) = await _cloudinaryProvider.UploadImageAsync(file);

            var updatedFields = user.UpdateProfilePicture(newUrl, newPublicId);
            if (updatedFields.Length > 0)
            {
                var userRepository = _unitOfWork.GetRepository<AppUser>();
                userRepository.UpdateFields(user, updatedFields);
                await _unitOfWork.SaveAsync();
            }

            return new ProfileImageResponseDTO { ProfilePictureUrl = newUrl };
        }

        public async Task DeleteProfileImageAsync()
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);

            if (string.IsNullOrEmpty(user.ProfilePictureUrl) && string.IsNullOrEmpty(user.ProfilePicturePublicId))
                return;

            if (!string.IsNullOrEmpty(user.ProfilePicturePublicId))
            {
                try
                {
                    bool deleted = await _cloudinaryProvider.DeleteImageAsync(user.ProfilePicturePublicId);
                    if (!deleted)
                        _logger.LogWarning("Failed to delete profile image with PublicId {PublicId} from Cloudinary for user {UserId}.", user.ProfilePicturePublicId, userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting profile image with PublicId {PublicId} from Cloudinary for user {UserId}.", user.ProfilePicturePublicId, userId);
                    throw new ErrorException(StatusCodes.Status500InternalServerError, ErrorCode.ServerError, "Lỗi xóa ảnh từ Cloudinary.");
                }
            }
            else
                _logger.LogWarning("Profile picture URL exists for user {UserId}, but PublicId is missing. Cannot delete from Cloudinary.", userId);

            var updatedFields = user.UpdateProfilePicture(null, null);
            if (updatedFields.Length > 0)
            {
                var userRepository = _unitOfWork.GetRepository<AppUser>();
                userRepository.UpdateFields(user, updatedFields);
                await _unitOfWork.SaveAsync();
            }
        }
    }
}