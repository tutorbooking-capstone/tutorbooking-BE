using App.DTOs.ScheduleDTOs;

namespace App.Services.Interfaces
{
    public interface IScheduleService
    {
        Task<List<DailyAvailabilityDTO>> GetTutorAvailabilityAsync(string tutorId, DateTime startDate, DateTime endDate);
        Task<WeeklyPatternResponse> UpdateWeeklyPatternAsync(UpdateWeeklyPatternRequest request);
        Task<List<WeeklyPatternResponse>> GetAllWeeklyPatternsAsync(string tutorId);
        Task<List<DailyAvailabilityPatternDTO>> GetWeekAvailabilityAsync(string tutorId, DateTime startDate);
        


        
        Task<WeeklyPatternResponse> CreateWeeklyPatternAsync(CreateWeeklyPatternRequest request);
        Task<WeeklyPatternDetailResponse> GetWeeklyPatternDetailAsync(string patternId);
        Task<List<WeeklyPatternWithDatesResponse>> GetWeeklyPatternsWithDatesAsync(string tutorId);
        Task<List<DailyScheduleResponse>> GetTutorScheduleAsync(string tutorId, DateTime startDate, DateTime endDate);
        Task<List<SlotRequest>> GetBlockedSlotsForPatternAsync(string patternId);
        Task<WeeklyPatternResponse> EditWeeklyPatternAsync(string patternId, List<SlotRequest> newSlots);
        Task<bool> CanDeleteWeeklyPatternAsync(string patternId);
        Task<Dictionary<string, object>> GetScheduleMetadataAsync();
        Task DeleteWeeklyPatternAsync(string patternId);
    }
}