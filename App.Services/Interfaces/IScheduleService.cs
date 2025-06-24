using App.DTOs.ScheduleDTOs;

namespace App.Services.Interfaces
{
    public interface IScheduleService
    {
        Task<List<DailyAvailabilityDTO>> GetTutorAvailabilityAsync(string tutorId, DateTime startDate, DateTime endDate);
        Task<WeeklyPatternResponse> UpdateWeeklyPatternAsync(UpdateWeeklyPatternRequest request);
        Task<List<WeeklyPatternResponse>> GetAllWeeklyPatternsAsync(string tutorId);
        Task<List<DailyAvailabilityPatternDTO>> GetWeekAvailabilityAsync(string tutorId, DateTime startDate);
        Task DeleteWeeklyPatternAsync(string patternId);
    }
}