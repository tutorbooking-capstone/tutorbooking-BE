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

        

        public async Task<BasePaginatedList<TutorIntroductionVideoResponse>> GetAsync(TutorIntroductionVideoStatus? status, string? userId,int page = 1, int size = 10)
        {
            var predicate = PredicateBuilder.New<TutorIntroductionVideo>();
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


        #region Staff
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
            entity.Status = request.Status;
            _unitOfWork.GetRepository<TutorIntroductionVideo>().Update(entity);
            await _unitOfWork.SaveAsync();

            // If the status is active, inactive all other active videos
            if (entity.Status == TutorIntroductionVideoStatus.Active)
            {
                var approvedEntities = await _unitOfWork.GetRepository<TutorIntroductionVideo>()
                    .ExistEntities()
                    .Where(e => e.Status.Equals(TutorIntroductionVideoStatus.Active) 
                    && !e.Id.Equals(entity.Id)
                    && e.TutorUserId.Equals(entity.TutorUserId))
                    .ToListAsync();
                if (approvedEntities.Count > 0)
                {
                    foreach (var approvedEntity in approvedEntities)
                    {
                        approvedEntity.Status = TutorIntroductionVideoStatus.Inactive;
                        _unitOfWork.GetRepository<TutorIntroductionVideo>().Update(approvedEntity);
                    }
                    await _unitOfWork.SaveAsync();
                }
            }
            return entity.ToResponse();
        }
        #endregion

        #region Tutor   
        public async Task<TutorIntroductionVideoResponse> CreateAsync(TutorIntroductionVideoRequest request)
        {
            var response = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var entity = request.ToEntity();
                entity.TutorUserId = _userService.GetCurrentUserId();

                _unitOfWork.GetRepository<TutorIntroductionVideo>().Insert(entity);
                await _unitOfWork.SaveAsync();

                return entity.ToResponse();
            });
            return response;
        }

        public async Task UpdateStatusAsync(TutorIntroductionVideoStatusUpdateRequest request)
        {
            var response = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () =>
            {
                var currentUserId = _userService.GetCurrentUserId();
                var entity = await _unitOfWork.GetRepository<TutorIntroductionVideo>()
                .ExistEntities()
                .FirstOrDefaultAsync(e => e.Id.Equals(request.Id) && e.TutorUserId.Equals(currentUserId));
                if (entity == null)
                    throw new ErrorException(
                        (int)StatusCode.NotFound,
                        ErrorCode.NotFound,
                        "NOT_FOUND");
                if (entity.Status == TutorIntroductionVideoStatus.Pending
                    || entity.Status == TutorIntroductionVideoStatus.Rejected)
                    throw new ErrorException(
                        (int)StatusCode.BadRequest,
                        ErrorCode.BadRequest,
                        "CANNOT_UPDATE_WHEN_STATUS_IS_PENDING_OR_REJECTED");

                entity.Status = request.Status;
                _unitOfWork.GetRepository<TutorIntroductionVideo>().Update(entity);
                await _unitOfWork.SaveAsync();

                // If the status is active, inactive all other active videos
                if (entity.Status == TutorIntroductionVideoStatus.Active)
                {
                    var approvedEntities = await _unitOfWork.GetRepository<TutorIntroductionVideo>()
                        .ExistEntities()
                        .Where(e => e.Status.Equals(TutorIntroductionVideoStatus.Active)
                        && !e.Id.Equals(entity.Id)
                        && e.TutorUserId.Equals(entity.TutorUserId))
                        .ToListAsync();
                    if (approvedEntities.Count > 0)
                    {
                        foreach (var approvedEntity in approvedEntities)
                        {
                            approvedEntity.Status = TutorIntroductionVideoStatus.Inactive;
                            _unitOfWork.GetRepository<TutorIntroductionVideo>().Update(approvedEntity);
                        }
                        await _unitOfWork.SaveAsync();
                    }
                }
                return true;
            });
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
        #endregion
    }
}
