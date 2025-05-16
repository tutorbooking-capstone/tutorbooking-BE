using App.Repositories.Models;
using App.Repositories.Models.Scheduling;
using App.Repositories.UoW;
using Microsoft.EntityFrameworkCore;
using App.Services.Interfaces;
using App.Core.Base;
using System.Linq.Expressions;
using App.Core.Utils;
using App.Repositories.Models.User;
using App.Core.Constants;
using Microsoft.AspNetCore.Http;

namespace App.Services.Services
{
    public class SeedService : ISeedService
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;

        public SeedService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        #endregion

        private async Task<List<T>> SeedResourceAsync<T>(
            string resourceName,
            List<T> seedData,
            Expression<Func<IUnitOfWork, IGenericRepository<T>>> repoSelector) where T : class
        {
            var repo = repoSelector.Compile()(_unitOfWork);
            
            try
            {
                repo.InsertRange(seedData);
                await _unitOfWork.SaveAsync();
                return seedData;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate") == true)
            {
                throw new AlreadySeededException(
                    resourceName: resourceName,
                    seededData: seedData
                );
            }
        }

        public async Task<List<Hashtag>> SeedHashtagsAsync()
        {
            var hashtags = HashtagSeeder.SeedHashtags();
            return await SeedResourceAsync(
                resourceName: "Hashtags",
                seedData: hashtags,
                repoSelector: uow => uow.GetRepository<Hashtag>()
            );
        }

        public async Task<WeeklyAvailabilityPattern> SeedTutorAvailabilityAsync(string tutorId)
        {
            // Check if tutor exists
            var tutorExists = await _unitOfWork.GetRepository<Tutor>()
                .ExistEntities()
                .AnyAsync(t => t.UserId == tutorId);

            if (!tutorExists)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    $"Tutor with ID {tutorId} not found.");

            // Create weekly pattern starting from today (using UTC)
            var pattern = new WeeklyAvailabilityPattern
            {
                TutorId = tutorId,
                AppliedFrom = DateTime.UtcNow.Date, // Use UTC date
                Slots = new List<AvailabilitySlot>()
            };

            // Generate a seed ID using the new SeedGuid method
            pattern.Id = HSeed.SeedGuid<WeeklyAvailabilityPattern>();

            // Generate availability slots for the pattern
            // For example, available on weekdays from 18:00 to 21:00 (slots 36-42)
            var slots = new List<AvailabilitySlot>();

            for (int day = 2; day <= 6; day++) // Monday to Friday
            {
                for (int slot = 36; slot < 42; slot++) // 18:00 to 21:00
                {
                    var availSlot = new AvailabilitySlot
                    {
                        Type = SlotType.Available,
                        DayInWeek = (DayInWeek)day,
                        SlotIndex = slot,
                        WeeklyPatternId = pattern.Id
                    };
                    
                    // Generate seed ID for the slot using the new SeedGuid method
                    availSlot.Id = HSeed.SeedGuid<AvailabilitySlot>();
                    slots.Add(availSlot);
                }
            }

            // Add some slots on weekends
            for (int day = 1; day <= 7; day += 6) // Sunday and Saturday
            {
                for (int slot = 20; slot < 36; slot += 2) // 10:00 to 18:00 with gaps
                {
                    var availSlot = new AvailabilitySlot
                    {
                        Type = SlotType.Available,
                        DayInWeek = (DayInWeek)day,
                        SlotIndex = slot,
                        WeeklyPatternId = pattern.Id
                    };
                    
                    availSlot.Id = HSeed.SeedGuid<AvailabilitySlot>();
                    slots.Add(availSlot);
                }
            }

            pattern.Slots = slots;

            try
            {
                _unitOfWork.GetRepository<WeeklyAvailabilityPattern>().Insert(pattern);
                await _unitOfWork.SaveAsync();
                return pattern;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate") == true)
            {
                throw new AlreadySeededException(
                    resourceName: "WeeklyAvailabilityPattern",
                    seededData: pattern
                );
            }
        }

        public async Task<List<BookingSlot>> SeedTutorBookingsAsync(string tutorId, List<string> learnerIds = null, int count = 3)
        {
            // Check if tutor exists
            var tutorExists = await _unitOfWork.GetRepository<Tutor>()
                .ExistEntities()
                .AnyAsync(t => t.UserId == tutorId);

            if (!tutorExists)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    $"Tutor with ID {tutorId} not found.");

            // Validate learner IDs if provided
            if (learnerIds != null && learnerIds.Any())
            {
                var validLearnerCount = await _unitOfWork.GetRepository<Learner>()
                    .ExistEntities()
                    .Where(l => learnerIds.Contains(l.UserId))
                    .CountAsync();

                if (validLearnerCount != learnerIds.Count)
                    throw new ErrorException(
                        StatusCodes.Status400BadRequest,
                        ErrorCode.BadRequest,
                        "One or more provided Learner IDs are invalid.");
            }

            // Get existing weekly pattern to link the booked slots
            var pattern = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>()
                .ExistEntities()
                .Include(p => p.Slots)
                .Where(p => p.TutorId == tutorId)
                .OrderByDescending(p => p.AppliedFrom)
                .FirstOrDefaultAsync();

            if (pattern == null || pattern.Slots == null || !pattern.Slots.Any())
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    "No availability pattern found for this tutor. Please seed availability pattern first.");

            var bookingSlots = new List<BookingSlot>();
            var random = new Random();
            
            // Get all available slots
            var availableSlots = pattern.Slots
                .Where(s => s.Type == SlotType.Available)
                .GroupBy(s => s.DayInWeek)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Use provided learner IDs or null if not provided
            var learnerIdQueue = learnerIds != null && learnerIds.Any() 
                ? new Queue<string>(learnerIds) 
                : null;

            // Create booking slots
            for (int i = 0; i < count && availableSlots.Any(); i++)
            {
                // Pick a random day with available slots
                var randomDay = availableSlots.Keys.ElementAt(random.Next(availableSlots.Count));
                var slotsForDay = availableSlots[randomDay];
                
                if (!slotsForDay.Any())
                    continue;

                // Take a consecutive group of slots (1-3 slots)
                var startIndex = random.Next(slotsForDay.Count);
                var slotCount = Math.Min(random.Next(1, 4), slotsForDay.Count - startIndex);
                var slotsToBook = slotsForDay.Skip(startIndex).Take(slotCount).ToList();
                
                // Get a learner ID from the queue, cycling through them if needed
                string learnerId = null;
                if (learnerIdQueue != null && learnerIdQueue.Any())
                {
                    if (learnerIdQueue.Count == 0)
                    {
                        // Refill the queue if we've used all IDs but need more
                        foreach (var id in learnerIds)
                            learnerIdQueue.Enqueue(id);
                    }
                    learnerId = learnerIdQueue.Dequeue();
                }

                // Calculate start date (next occurrence of the day of week) in UTC
                var todayUtc = DateTime.UtcNow.Date;
                int daysToAdd = ((int)randomDay - (int)todayUtc.DayOfWeek + 7) % 7;
                if (daysToAdd == 0) daysToAdd = 7; // Next week if today is the same day
                var startDate = todayUtc.AddDays(daysToAdd);

                // Create booking
                var booking = new BookingSlot
                {
                    TutorId = tutorId,
                    LearnerId = learnerId, // Use real learnerId or null
                    Note = $"Seed booking #{i+1}",
                    StartDate = startDate, // UTC date
                    RepeatForWeeks = random.Next(0, 4) // 0-3 weeks repeat
                };

                // Generate seed ID using the new SeedGuid method
                booking.Id = HSeed.SeedGuid<BookingSlot>();

                bookingSlots.Add(booking);
                
                // Store which slots to update for this booking
                booking.Slots = slotsToBook;
                
                // Remove used slots from available slots
                availableSlots[randomDay] = slotsForDay.Except(slotsToBook).ToList();
                if (!availableSlots[randomDay].Any())
                    availableSlots.Remove(randomDay);
            }

            try
            {
                // First insert the booking slots without saving yet
                _unitOfWork.GetRepository<BookingSlot>().InsertRange(bookingSlots);
                
                // IMPORTANT: Execute in a single transaction to maintain referential integrity
                await _unitOfWork.ExecuteInTransactionAsync(async () => {
                    // Now update the availability slots to point to the booking slots
                    foreach (var booking in bookingSlots)
                    {
                        foreach (var slot in booking.Slots)
                        {
                            slot.Type = SlotType.Booked;
                            slot.BookingSlotId = booking.Id;
                        }
                    }
                    
                    await _unitOfWork.SaveAsync();
                    return bookingSlots;
                });
                
                return bookingSlots;
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("duplicate") == true)
            {
                throw new AlreadySeededException(
                    resourceName: "BookingSlots",
                    seededData: bookingSlots
                );
            }
        }
    }
} 