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

            // Create weekly pattern starting from today
            var pattern = new WeeklyAvailabilityPattern
            {
                TutorId = tutorId,
                AppliedFrom = DateTime.Today,
                Slots = new List<AvailabilitySlot>()
            };

            // Generate a seed ID for the pattern
            pattern.Id = pattern.SeedId(x => $"{x.TutorId}_{x.AppliedFrom:yyyyMMdd}");

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
                    
                    // Generate seed ID for the slot
                    availSlot.Id = availSlot.SeedId(x => $"{pattern.Id}_{(int)x.DayInWeek}_{x.SlotIndex}");
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
                    
                    availSlot.Id = availSlot.SeedId(x => $"{pattern.Id}_{(int)x.DayInWeek}_{x.SlotIndex}");
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

        public async Task<List<BookingSlot>> SeedTutorBookingsAsync(string tutorId, int count = 3)
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
                
                // Create mock learnerId
                string learnerId = $"learner_{Guid.NewGuid().ToString().Substring(0, 8)}";

                // Calculate start date (next occurrence of the day of week)
                var today = DateTime.Today;
                int daysToAdd = ((int)randomDay - (int)today.DayOfWeek + 7) % 7;
                if (daysToAdd == 0) daysToAdd = 7; // Next week if today is the same day
                var startDate = today.AddDays(daysToAdd);

                // Create booking
                var booking = new BookingSlot
                {
                    TutorId = tutorId,
                    LearnerId = learnerId,
                    Note = $"Seed booking #{i+1}",
                    StartDate = startDate,
                    RepeatForWeeks = random.Next(0, 4) // 0-3 weeks repeat
                };

                // Generate seed ID
                booking.Id = booking.SeedId(x => $"{x.TutorId}_{x.StartDate:yyyyMMdd}_{i}");

                // Update availability slots to point to this booking
                foreach (var slot in slotsToBook)
                {
                    slot.Type = SlotType.Booked;
                    slot.BookingSlotId = booking.Id;
                }

                bookingSlots.Add(booking);
                
                // Remove used slots from available slots
                availableSlots[randomDay] = slotsForDay.Except(slotsToBook).ToList();
                if (!availableSlots[randomDay].Any())
                    availableSlots.Remove(randomDay);
            }

            try
            {
                // Update availability slots
                await _unitOfWork.SaveAsync();

                // Insert bookings
                _unitOfWork.GetRepository<BookingSlot>().InsertRange(bookingSlots);
                await _unitOfWork.SaveAsync();
                
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