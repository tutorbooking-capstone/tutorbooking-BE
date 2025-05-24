using App.Repositories.Models;
using App.Repositories.Models.Scheduling;
using App.Repositories.UoW;
using Microsoft.EntityFrameworkCore;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using App.Core.Base;
using System.Linq.Expressions;
using App.Core.Utils;
using App.Repositories.Models.User;
using App.Core.Constants;
using Microsoft.AspNetCore.Http;
using App.DTOs.AuthDTOs;
using Microsoft.Extensions.Logging;
using App.DTOs.AppUserDTOs.TutorDTOs;

namespace App.Services.Services
{
    public class SeedService : ISeedService
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAuthService _authService;
        private readonly ILogger<SeedService> _logger;
        private readonly ITutorService _tutorService;

        public SeedService(
            IUnitOfWork unitOfWork,
            IAuthService authService,
            ILogger<SeedService> logger,
            ITutorService tutorService)
        {
            _unitOfWork = unitOfWork;
            _authService = authService;
            _logger = logger;
            _tutorService = tutorService;
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
    
        public async Task<List<string>> SeedUsersAsync(string emailPrefix, int count)
        {
            if (string.IsNullOrWhiteSpace(emailPrefix))
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Email prefix cannot be empty");

            if (count <= 0)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Count must be greater than 0");

            var createdEmails = new List<string>();
            // Standard password for all seeded accounts - should match validation requirements
            const string defaultPassword = "Password123!";

            for (int i = 1; i <= count; i++)
            {
                string email = $"{emailPrefix}{i}@gmail.com";
                
                try
                {
                    var registerRequest = new RegisterRequest
                    {
                        Email = email,
                        Password = defaultPassword,
                        ConfirmPassword = defaultPassword
                    };

                    await _authService.SeedRegisterAsync(registerRequest);
                    createdEmails.Add(email);
                    
                    // Log to console as requested
                    _logger.LogInformation($"Seeded user account: {email}");
                    Console.WriteLine($"Seeded user account: {email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to seed user account: {email}");
                    Console.WriteLine($"Failed to seed user account: {email} - Error: {ex.Message}");
                }
            }

            return createdEmails;
        }

        public async Task<List<string>> SeedTutorsAsync(string emailPrefix, int count)
        {
            if (string.IsNullOrWhiteSpace(emailPrefix))
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Email prefix cannot be empty");

            if (count <= 0)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Count must be greater than 0");

            // Find existing users with the given prefix instead of creating new ones
            var existingEmails = Enumerable.Range(1, count)
                .Select(i => $"{emailPrefix}{i}@gmail.com")
                .ToList();
            
            var existingUsers = await _unitOfWork.GetRepository<AppUser>()
                .ExistEntities()
                .Where(u => existingEmails.Contains(u.Email))
                .ToListAsync();
            
            if (!existingUsers.Any())
            {
                _logger.LogWarning($"No users found with email prefix '{emailPrefix}'");
                return new List<string>();
            }
            
            var registeredTutors = new List<string>();
            
            // Get hashtags for use in tutor registration
            var hashtags = HashtagSeeder.SeedHashtags();
            var hashtagIds = hashtags.Select(h => h.Id).ToList();
            
            // Define some common languages with their ISO codes
            var languageCodes = new List<string> { "en", "vi", "zh", "ja", "ko", "fr", "de", "es" };
            var random = new Random();

            foreach (var user in existingUsers)
            {
                try
                {
                    // Check if user is already a tutor
                    var existingTutor = await _unitOfWork.GetRepository<Tutor>()
                        .ExistEntities()
                        .AnyAsync(t => t.UserId == user.Id);
                        
                    if (existingTutor)
                    {
                        _logger.LogInformation($"User {user.Email} is already a tutor, skipping");
                        continue;
                    }

                    // Prepare tutor registration data
                    var nickName = $"Tutor{user.Email.Split('@')[0].Replace(emailPrefix, "")}";
                    var request = new TutorRegistrationRequest(
                        // Basic info
                        FullName: $"Tutor {nickName}",
                        DateOfBirth: DateTime.UtcNow.AddYears(-random.Next(25, 50)),
                        Gender: (Gender)(random.Next(0, 3)),
                        Timezone: "UTC+7",
                        
                        // Tutor info
                        NickName: nickName,
                        Brief: $"I am {nickName}, an experienced tutor ready to help you learn.",
                        Description: $"As {nickName}, I have been teaching for {random.Next(1, 10)} years. I specialize in creating engaging lessons tailored to student needs.",
                        TeachingMethod: "Interactive, student-centered approach with regular progress assessments.",
                        
                        // Hashtags - select 2-5 random hashtags
                        HashtagIds: hashtagIds
                            .OrderBy(_ => random.Next())
                            .Take(random.Next(2, 6))
                            .ToList(),
                        
                        // Languages - select 1-3 random languages
                        Languages: GenerateRandomLanguages(languageCodes, random.Next(1, 4), random)
                    );

                    // Use the tutor service to register the tutor using the seed method
                    await _tutorService.SeedRegisterAsTutorAsync(user.Id, request);
                    registeredTutors.Add(user.Email);
                    
                    // Log success
                    _logger.LogInformation($"Registered user as tutor: {user.Email}");
                    Console.WriteLine($"Registered user as tutor: {user.Email}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to register user as tutor: {user.Email}");
                    Console.WriteLine($"Failed to register user as tutor: {user.Email} - Error: {ex.Message}");
                }
            }

            return registeredTutors;
        }

        // Helper method to generate random language proficiencies
        private List<TutorLanguageDTO> GenerateRandomLanguages(List<string> languageCodes, int count, Random random)
        {
            var selectedCodes = languageCodes
                .OrderBy(_ => random.Next())
                .Take(count)
                .ToList();
            
            var languages = new List<TutorLanguageDTO>();
            bool hasPrimary = false;
            
            foreach (var code in selectedCodes)
            {
                // Decide if this should be the primary language (only one can be primary)
                bool isPrimary = !hasPrimary && random.Next(2) == 1;
                if (isPrimary) hasPrimary = true;
                
                languages.Add(new TutorLanguageDTO
                {
                    LanguageCode = code,
                    Proficiency = random.Next(3, 8), // 3-7 proficiency level
                    IsPrimary = isPrimary
                });
            }
            
            // Ensure at least one language is marked as primary
            if (!hasPrimary && languages.Any())
            {
                languages[0].IsPrimary = true;
            }
            
            return languages;
        }

        public async Task<int> SeedAllTutorDetailsAsync(string tutorPrefix, string learnerPrefix, int tutorCount, int learnerCount)
        {
            if (string.IsNullOrWhiteSpace(tutorPrefix) || string.IsNullOrWhiteSpace(learnerPrefix))
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Prefix values cannot be empty");

            if (tutorCount <= 0 || learnerCount <= 0)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Count values must be greater than 0");
            
            // Generate tutor and learner emails
            var tutorEmails = Enumerable.Range(1, tutorCount)
                .Select(i => $"{tutorPrefix}{i}@gmail.com")
                .ToList();
            
            var learnerEmails = Enumerable.Range(1, learnerCount)
                .Select(i => $"{learnerPrefix}{i}@gmail.com")
                .ToList();
            
            // Get tutor and learner IDs from the database
            var tutors = await _unitOfWork.GetRepository<AppUser>()
                .ExistEntities()
                .Where(u => tutorEmails.Contains(u.Email))
                .Join(_unitOfWork.GetRepository<Tutor>().ExistEntities(),
                    user => user.Id,
                    tutor => tutor.UserId,
                    (user, tutor) => new { User = user, Tutor = tutor })
                .ToListAsync();
            
            var learners = await _unitOfWork.GetRepository<AppUser>()
                .ExistEntities()
                .Where(u => learnerEmails.Contains(u.Email))
                .Join(_unitOfWork.GetRepository<Learner>().ExistEntities(),
                    user => user.Id,
                    learner => learner.UserId,
                    (user, learner) => new { User = user, Learner = learner })
                .ToListAsync();
            
            int tutorsProcessed = 0;
            var random = new Random();
            
            foreach (var tutor in tutors)
            {
                try
                {
                    // Seed availability pattern
                    await SeedTutorAvailabilityAsync(tutor.Tutor.UserId);
                    
                    // Select random 3-7 learners
                    var selectedLearnerCount = random.Next(3, Math.Min(8, learners.Count + 1));
                    var selectedLearnerIds = learners
                        .OrderBy(_ => random.Next())
                        .Take(selectedLearnerCount)
                        .Select(l => l.Learner.UserId)
                        .ToList();
                    
                    // Seed bookings with these learners
                    var bookingCount = random.Next(3, 7); // Random 3-6 bookings
                    await SeedTutorBookingsAsync(tutor.Tutor.UserId, selectedLearnerIds, bookingCount);
                    
                    tutorsProcessed++;
                    
                    // Log progress
                    Console.WriteLine($"Seeded details for tutor: {tutor.User.Email} ({tutorsProcessed}/{tutors.Count})");
                    _logger.LogInformation($"Seeded details for tutor: {tutor.User.Email} ({tutorsProcessed}/{tutors.Count})");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to seed details for tutor: {tutor.User.Email}");
                    Console.WriteLine($"Failed to seed details for tutor: {tutor.User.Email} - Error: {ex.Message}");
                }
            }
            
            return tutorsProcessed;
        }
    }
} 