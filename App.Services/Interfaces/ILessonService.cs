using App.DTOs.LessonDTOs;

namespace App.Services.Interfaces
{
    public interface ILessonService
    {
        Task<List<LessonResponse>> GetAllLessonsByTutorAsync(string tutorId);
        Task<LessonResponse> GetLessonByIdAsync(string lessonId);
        Task<LessonResponse> CreateLessonAsync(CreateLessonRequest request);
        Task<LessonResponse> UpdateLessonAsync(string lessonId, UpdateLessonRequest request);
        Task DeleteLessonAsync(string lessonId);
    }
}
