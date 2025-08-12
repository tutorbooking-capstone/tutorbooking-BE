using App.Core.Base;
using App.Core.Constants;
using App.DTOs.AppUserDTOs.TutorDTOs;
using App.Repositories.Models;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using System.Data.Entity;


namespace App.Services.Services
{
    public class TutorIntroductionVideoService : ITutorIntroductionVideoService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;

        public TutorIntroductionVideoService(IUnitOfWork unitOfWork, IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
        }

        public async Task<TutorIntroductionVideoResponse> CreateAsync(TutorIntroductionVideoRequest request)
        {
            var response = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var entity = request.ToEntity();
                _unitOfWork.GetRepository<TutorIntroductionVideo>().Insert(entity);
                await _unitOfWork.SaveAsync();

                #region delete other pending entities
                var pendingEntities = await _unitOfWork.GetRepository<TutorIntroductionVideo>()
                    .ExistEntities()
                    .Where(e => e.Status.Equals(TutorIntroductionVideoStatus.Pending))
                    .ToArrayAsync();
                if (pendingEntities.Length > 0)
                {
                    _unitOfWork.GetRepository<TutorIntroductionVideo>().DeleteRange(pendingEntities);
                    await _unitOfWork.SaveAsync();
                }
                #endregion

                return entity.ToResponse();
            });
            return response;
        }

        public async Task<ICollection<TutorIntroductionVideoResponse>> GetPendingAsync(int page, int size)
        {
            return await _unitOfWork.GetRepository<TutorIntroductionVideo>().ExistEntities()
                .Where(e => e.Status == TutorIntroductionVideoStatus.Pending)
                .OrderBy(e => e.CreatedTime)
                .Select(e => e.ToResponse())
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
        }

        public async Task<TutorIntroductionVideoResponse?> GetByIdAsync(string id)
        {
            var entity = await _unitOfWork.GetRepository<TutorIntroductionVideo>().ExistEntities()
                .Where(e => e.Id.Equals(id))
                .Select(e => e.ToResponse())
                .FirstOrDefaultAsync();
            if (entity == null)
                throw new ErrorException(
                    (int)StatusCode.NotFound,
                    ErrorCode.NotFound,
                    "NOT_FOUND");
            return entity;
        }

        public async Task<ICollection<TutorIntroductionVideoResponse>> GetByCurrentUserIdAsync(int page = 1, int size = 10)
        {
            var user = _userService.GetCurrentUserId();

            return await _unitOfWork.GetRepository<TutorIntroductionVideo>().ExistEntities()
                .Where(e => e.TutorUserId.Equals(user))
                .OrderByDescending(e => e.CreatedTime)
                .Select(e => e.ToResponse())
                .Skip((page - 1) * size)
                .Take(size)
                .ToListAsync();
        }

        public async Task<TutorIntroductionVideoResponse> ReviewAsync(TutorIntroductionVideoReviewRequest request)
        {
            var entity = await _unitOfWork.GetRepository<TutorIntroductionVideo>()
                .ExistEntities()
                .FirstOrDefaultAsync(e => e.Id == request.Id);
            if (entity == null)
                throw new ErrorException(
                    (int)StatusCode.NotFound,
                    ErrorCode.NotFound,
                    "NOT_FOUND");
            entity.Review(ref request);
            _unitOfWork.GetRepository<TutorIntroductionVideo>().Update(entity);
            await _unitOfWork.SaveAsync();
            return entity.ToResponse();
        }

        public async Task DeleteAsync(string id)
        {
            var entity = await _unitOfWork.GetRepository<TutorIntroductionVideo>()
                .ExistEntities()
                .FirstOrDefaultAsync(e => e.Id == id);
            if (entity == null)
                throw new ErrorException(
                    (int)StatusCode.NotFound,
                    ErrorCode.NotFound,
                    "NOT_FOUND");
            _unitOfWork.GetRepository<TutorIntroductionVideo>().Delete(entity);
            await _unitOfWork.SaveAsync();
        }
    }
}
