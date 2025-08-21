using App.Core.Base;
using App.Core.Constants;
using App.DTOs.AppUserDTOs.TutorDTOs;
using App.Repositories.Models;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using LinqKit;
using Microsoft.EntityFrameworkCore;


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
                entity.TutorUserId = _userService.GetCurrentUserId();

                _unitOfWork.GetRepository<TutorIntroductionVideo>().Insert(entity);
                await _unitOfWork.SaveAsync();

                #region delete other pending entities
                var pendingEntities = await _unitOfWork.GetRepository<TutorIntroductionVideo>()
                    .ExistEntities()
                    .Where(e => e.Status.Equals(TutorIntroductionVideoStatus.Pending) && !e.Id.Equals(entity.Id))
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


        public async Task<BasePaginatedList<TutorIntroductionVideoResponse>> GetAsync(TutorIntroductionVideoStatus? status, string? userId,int page = 1, int size = 10)
        {
            var predicate = PredicateBuilder.New<TutorIntroductionVideo>(e => e.Status == TutorIntroductionVideoStatus.Pending);
            if (status != null)
                predicate = predicate.And(e => e.Status == status);
            if (userId != null)
                predicate = predicate.And(e => e.TutorUserId.Equals(userId));

            var response = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var totalCount = await _unitOfWork.GetRepository<TutorIntroductionVideo>().ExistEntities()
                .Where(predicate)
                .CountAsync();

                var result = await _unitOfWork.GetRepository<TutorIntroductionVideo>().ExistEntities()
               .Where(predicate)
               .OrderBy(e => e.CreatedTime)
               .Select(e => e.ToResponse())
               .Skip((page - 1) * size)
               .Take(size)
               .ToListAsync();
                return new BasePaginatedList<TutorIntroductionVideoResponse>(result, totalCount, page - 1, size);
            });
            return response;
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

        public async Task<BasePaginatedList<TutorIntroductionVideoResponse>> GetByCurrentUserIdAsync(TutorIntroductionVideoStatus? status, int page = 1, int size = 10)
        {
            return await GetAsync(status, _userService.GetCurrentUserId(), page, size);
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
