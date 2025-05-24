using App.DTOs.AppUserDTOs.TutorDTOs;
namespace App.Services.Interfaces
{
    public interface IScheduleService
    {
        // Retrieve methods
        Task<List<WeeklyAvailabilityPatternDTO>> GetTutorAvailabilityPatternsAsync(string tutorId);
        Task<List<BookingSlotDTO>> GetTutorBookingSlotsAsync(string tutorId);
        
        // Seed methods
        //Task<WeeklyAvailabilityPattern> SeedTutorAvailabilityAsync(string tutorId);
        //Task<List<BookingSlot>> SeedTutorBookingsAsync(string tutorId, int count = 3);

        // New methods
        Task<WeeklyAvailabilityPatternDTO> CreateWeeklyAvailabilityPatternAsync(WeeklyAvailabilityPatternDTO patternDto);
        Task<bool> DeleteWeeklyAvailabilityPatternAsync(string patternId);
        Task<List<DailyAvailabilityDTO>> GetAvailabilityForWeekAsync(string tutorId, DateTime weekStartDate);
    }
}