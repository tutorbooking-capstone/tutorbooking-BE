using App.Repositories.Models.Scheduling;
using System.Collections.Generic;
namespace App.Services.Interfaces
{
    public interface IScheduleService
    {
        // Retrieve methods
        Task<List<WeeklyAvailabilityPattern>> GetTutorAvailabilityPatternsAsync(string tutorId);
        Task<List<BookingSlot>> GetTutorBookingSlotsAsync(string tutorId);
        
        // Seed methods
        //Task<WeeklyAvailabilityPattern> SeedTutorAvailabilityAsync(string tutorId);
        //Task<List<BookingSlot>> SeedTutorBookingsAsync(string tutorId, int count = 3);
    }
}