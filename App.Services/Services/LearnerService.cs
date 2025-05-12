using App.Core.Base;
using App.Core.Constants;
using App.DTOs.AppUserDTOs.LearnerDTOs;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services
{
    public class LearnerService : ILearnerService
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;

        public LearnerService(
            IUnitOfWork unitOfWork,
            IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
        }
        #endregion

        #region Private Helper
        private async Task<Learner> GetLearnerAsync(string userId)
        {
            var learnerRepo = _unitOfWork.GetRepository<Learner>();
            var learner = await learnerRepo.ExistEntities()
                .FirstOrDefaultAsync(l => l.UserId == userId);

            if (learner == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "Không tìm thấy thông tin học viên.");

            return learner;
        }
        #endregion

        public async Task UpdateLearningLanguageAsync(UpdateLearnerLanguageRequest request)
        {
            var userId = _userService.GetCurrentUserId();
            var learner = await GetLearnerAsync(userId);

            var updatedFields = learner.UpdateLearningLanguage(
                request.LanguageCode,
                request.ProficiencyLevel
            );

            if (updatedFields.Length > 0)
            {
                var learnerRepo = _unitOfWork.GetRepository<Learner>();
                learnerRepo.UpdateFields(learner, updatedFields);
                await _unitOfWork.SaveAsync();
            }
        }

        public async Task<(string LanguageCode, int ProficiencyLevel)> GetLearningLanguageAsync()
        {
            var userId = _userService.GetCurrentUserId();
            var learner = await GetLearnerAsync(userId);
            
            return (learner.LanguageCode, learner.ProficiencyLevel);
        }
    }
}
