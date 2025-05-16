using App.Repositories.Models;
using App.Repositories.Models.Scheduling;

namespace App.Services.Interfaces
{
    public interface ISeedService
    {
        Task<List<Hashtag>> SeedHashtagsAsync();
        
        // New seed methods
        Task<WeeklyAvailabilityPattern> SeedTutorAvailabilityAsync(string tutorId);
        Task<List<BookingSlot>> SeedTutorBookingsAsync(string tutorId, int count = 3);
    }
}