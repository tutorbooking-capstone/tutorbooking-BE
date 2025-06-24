using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.LessonDTOs;
using App.Repositories.Models;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services
{
    public class LessonService : ILessonService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserProvider _currentUserProvider;

        public LessonService(IUnitOfWork unitOfWork, ICurrentUserProvider currentUserProvider)
        {
            _unitOfWork = unitOfWork;
            _currentUserProvider = currentUserProvider;
        }

        #region Private Helpers
        private async Task<Lesson> GetLessonEntityByIdAsync(string lessonId, bool checkOwner = false)
        {
            var lesson = await _unitOfWork.GetRepository<Lesson>().ExistEntities()
                .FirstOrDefaultAsync(l => l.Id == lessonId);

            if (lesson == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Lesson not found.");

            if (checkOwner)
            {
                var currentUserId = _currentUserProvider.GetCurrentUserId();
                if (currentUserId is null || lesson.TutorId != currentUserId)
                    throw new ErrorException(
                        StatusCodes.Status403Forbidden, 
                        ErrorCode.Forbidden, 
                        "You are not authorized to perform this action on this lesson.");
            }

            return lesson;
        }

        private async Task<string> CheckTutorExists(string? tutorId)
        {
            if (tutorId is null)
                throw new ErrorException(
                    StatusCodes.Status401Unauthorized,
                    ErrorCode.Unauthorized,
                    "User is not authenticated.");

            var tutorExists = await _unitOfWork.GetRepository<Tutor>().ExistEntities().AnyAsync(t => t.UserId == tutorId);
            if (!tutorExists)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Tutor not found.");

            return tutorId;
        }
        #endregion

        public async Task<LessonResponse> CreateLessonAsync(CreateLessonRequest request)
        {
            var tutorId = await CheckTutorExists(_currentUserProvider.GetCurrentUserId());
            var lesson = request.ToLessonEntity(tutorId);

            _unitOfWork.GetRepository<Lesson>().Insert(lesson);
            await _unitOfWork.SaveAsync();

            return await _unitOfWork.GetRepository<Lesson>()
                .ExistEntities()
                .Where(l => l.Id == lesson.Id)
                .Select(LessonResponse.Projection)
                .FirstAsync();
        }

        public async Task DeleteLessonAsync(string lessonId)
        {
            var lesson = await GetLessonEntityByIdAsync(lessonId, checkOwner: true);
            _unitOfWork.GetRepository<Lesson>().Delete(lesson);
            await _unitOfWork.SaveAsync();
        }

        public async Task<List<LessonResponse>> GetAllLessonsByTutorAsync(string tutorId)
        {
            await CheckTutorExists(tutorId);

            return await _unitOfWork.GetRepository<Lesson>()
                .ExistEntities()
                .Where(l => l.TutorId == tutorId)
                .Select(LessonResponse.Projection)
                .ToListAsync();
        }

        public async Task<LessonResponse> GetLessonByIdAsync(string lessonId)
        {
            var lesson = await _unitOfWork.GetRepository<Lesson>()
                .ExistEntities()
                .Where(l => l.Id == lessonId)
                .Select(LessonResponse.Projection)
                .FirstOrDefaultAsync();

            if (lesson == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound, 
                    ErrorCode.NotFound, 
                    "Lesson not found.");

            return lesson;
        }

        public async Task<LessonResponse> UpdateLessonAsync(string lessonId, UpdateLessonRequest request)
        {
            var lesson = await GetLessonEntityByIdAsync(lessonId, checkOwner: true);
            lesson.UpdateLessonEntity(request);

            _unitOfWork.GetRepository<Lesson>().Update(lesson);
            await _unitOfWork.SaveAsync();

            return await GetLessonByIdAsync(lessonId);
        }
    }
}
