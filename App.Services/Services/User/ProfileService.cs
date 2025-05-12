using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.UserDTOs;
using App.Repositories.Models.User;
using App.Repositories.UoW;  
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

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
            if (file == null || file.Length == 0)
                throw new InvalidArgumentException(nameof(file), "Vui lòng chọn một tệp ảnh.");

            if (!file.ContentType.StartsWith("image/"))
                throw new InvalidArgumentException(nameof(file), "Tệp tải lên không phải là ảnh hợp lệ.");

            var userId = _userService.GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);

            if (!string.IsNullOrEmpty(user.ProfilePicturePublicId))
                await _cloudinaryProvider.DeleteImageAsync(user.ProfilePicturePublicId);

            var (newUrl, newPublicId) = await _cloudinaryProvider.UploadImageAsync(file);

            var updatedFields = user.UpdateBasicInformationPicture(newUrl, newPublicId);
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
                await _cloudinaryProvider.DeleteImageAsync(user.ProfilePicturePublicId);
            else
                _logger.LogWarning("Profile picture URL exists for user {UserId}, but PublicId is missing. Cannot delete from Cloudinary.", userId);

            var updatedFields = user.UpdateBasicInformationPicture(null, null);
            if (updatedFields.Length > 0)
            {
                var userRepository = _unitOfWork.GetRepository<AppUser>();
                userRepository.UpdateFields(user, updatedFields);
                await _unitOfWork.SaveAsync();
            }
        }

        public async Task UpdateFullNameAsync(string fullName)
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);

            var updatedFields = user.UpdateFullName(fullName);
            if (updatedFields.Length > 0)
            {
                var userRepository = _unitOfWork.GetRepository<AppUser>();
                userRepository.UpdateFields(user, updatedFields);
                await _unitOfWork.SaveAsync();
            }
        }

        public async Task UpdateDateOfBirthAsync(DateTime? dateOfBirth)
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);

            var updatedFields = user.UpdateDateOfBirth(dateOfBirth);
            if (updatedFields.Length > 0)
            {
                var userRepository = _unitOfWork.GetRepository<AppUser>();
                userRepository.UpdateFields(user, updatedFields);
                await _unitOfWork.SaveAsync();
            }
        }

        public async Task UpdateGenderAsync(Gender gender)
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);

            var updatedFields = user.UpdateGender(gender);
            if (updatedFields.Length > 0)
            {
                var userRepository = _unitOfWork.GetRepository<AppUser>();
                userRepository.UpdateFields(user, updatedFields);
                await _unitOfWork.SaveAsync();
            }
        }

        public async Task UpdateBasicInformationAsync(UpdateBasicInformationRequest request)
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);

            var updatedFields = user.UpdateBasicInformation(
                request.FullName,
                request.DateOfBirth,
                request.Gender,
                request.Timezone
            );

            if (updatedFields.Length > 0)
            {
                var userRepository = _unitOfWork.GetRepository<AppUser>();
                userRepository.UpdateFields(user, updatedFields);
                await _unitOfWork.SaveAsync();
            }
        }

        public async Task<UserProfileResponse> GetUserProfileAsync()
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _userService.GetUserByIdAsync(userId);
            
            // Get learner info if it exists
            var learnerRepository = _unitOfWork.GetRepository<Learner>();
            var learner = await learnerRepository.ExistEntities()
                .FirstOrDefaultAsync(l => l.UserId == userId);
            
            return user.ToUserProfileResponse(learner);
        }
    }
}