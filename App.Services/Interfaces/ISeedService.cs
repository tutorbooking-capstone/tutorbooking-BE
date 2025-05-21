using App.Repositories.Models;
using App.Repositories.Models.Scheduling;

namespace App.Services.Interfaces
{
    public interface ISeedService
    {
        Task<List<Hashtag>> SeedHashtagsAsync();
        
        // New seed methods
        Task<WeeklyAvailabilityPattern> SeedTutorAvailabilityAsync(string tutorId);
        Task<List<BookingSlot>> SeedTutorBookingsAsync(string tutorId, List<string> learnerIds = null, int count = 3);
        
        // New method for seeding user accounts
        Task<List<string>> SeedUsersAsync(string emailPrefix, int count);
        Task<List<string>> SeedTutorsAsync(string emailPrefix, int count);

        Task<int> SeedAllTutorDetailsAsync(string tutorPrefix, string learnerPrefix, int tutorCount, int learnerCount);
    }
}