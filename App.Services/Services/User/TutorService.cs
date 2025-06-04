using App.Core.Base;
using App.DTOs.AppUserDTOs.TutorDTOs;
using App.DTOs.BlogDTOs;
using App.DTOs.HashtagDTOs;
using App.Repositories.Models;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using App.Core.Constants;
using App.Repositories.Models.Papers;
using Microsoft.Extensions.Logging;
using App.Core.Mapper;
using App.Repositories.Models.Scheduling;

namespace App.Services.Services.User
{
    public partial class TutorService : ITutorService
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;
        private readonly ILogger<TutorService> _logger;

        public TutorService(
            IUnitOfWork unitOfWork,
            IUserService userService,
            ILogger<TutorService> logger)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
            _logger = logger;
        }
        #endregion

        #region Private Helpers
        private async Task<AppUser> GetUserToCreateTutor(string userId)
        {
            var appUser = await _userService.GetCurrentUserAsync();
            
            // Check if tutor already exists
            var existingTutor = await _unitOfWork.GetRepository<Tutor>()
                .ExistEntities()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (existingTutor != null)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Người dùng đã đăng ký làm gia sư");

            return appUser;
        }

        private async Task ProcessHashtags(string userId, List<string>? hashtagIds)
        {
            if (hashtagIds == null || !hashtagIds.Any()) return;

            await UpdateTutorHashtagsAsync(
                userId, 
                new UpdateTutorHashtagListRequest 
                { 
                    HashtagIds = hashtagIds 
                }
            );
        }

        private async Task<Tutor> GetTutorByIdAsync(string tutorId, bool includeUser = false)
        {
            var query = _unitOfWork.GetRepository<Tutor>()
                .ExistEntities();

            if (includeUser)
                query = query.Include(t => t.User);

            var tutor = await query.FirstOrDefaultAsync(t => t.UserId == tutorId.Trim());

            if (tutor == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    $"Tutor with ID {tutorId} not found.");

            return tutor;
        }

        private async Task ValidateHashtagIdsAsync(List<string> hashtagIds)
        {
            if (hashtagIds == null || !hashtagIds.Any()) return;

            var distinctIds = hashtagIds.Distinct().ToList();
            var validCount = await _unitOfWork.GetRepository<Hashtag>()
                .ExistEntities()
                .CountAsync(h => distinctIds.Contains(h.Id));

            if (validCount != distinctIds.Count)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "One or more provided Hashtag IDs are invalid.");
        }

        private async Task UpdateTutorLanguagesAsync(string tutorId, List<TutorLanguageDTO> requestedLanguages)
        {
            var tutorLanguageRepo = _unitOfWork.GetRepository<TutorLanguage>();
            var existingLanguages = await tutorLanguageRepo.ExistEntities()
                .Where(tl => tl.TutorId == tutorId)
                .ToListAsync();

            if (requestedLanguages == null || !requestedLanguages.Any())
                throw new ErrorException(
                    StatusCodes.Status400BadRequest, 
                    ErrorCode.BadRequest, 
                    "Gia sư phải đảm nhiệm ít nhất 1 ngôn ngữ để dạy.");

            // Validate Primary Language Rule (Can have any or onnly one???)

            var existingLangMap = existingLanguages.ToDictionary(l => l.LanguageCode);
            var requestedLangMap = requestedLanguages.ToDictionary(l => l.LanguageCode);

            var languagesToDelete = existingLanguages
                .Where(l => !requestedLangMap.ContainsKey(l.LanguageCode)).ToList();
            
            if (languagesToDelete.Any())
                tutorLanguageRepo.DeleteRange(languagesToDelete, false);

            var languagesToAdd = new List<TutorLanguage>();
            
            foreach (var requestedLang in requestedLanguages)
            {
                if (existingLangMap.TryGetValue(requestedLang.LanguageCode, out var existingLang))
                {
                    UpdateExistingLanguageIfNeeded(existingLang, requestedLang);
                    continue;
                }

                var newLang = requestedLang.ToEntity(tutorId);
                languagesToAdd.Add(newLang);
            }

            if (languagesToAdd.Any())
                tutorLanguageRepo.InsertRange(languagesToAdd);
        }

        private void UpdateExistingLanguageIfNeeded(TutorLanguage existingLang, TutorLanguageDTO requestedLang)
        {
            if (existingLang.IsPrimary == requestedLang.IsPrimary && 
                existingLang.Proficiency == requestedLang.Proficiency)
                return;

            existingLang.IsPrimary = requestedLang.IsPrimary;
            existingLang.Proficiency = requestedLang.Proficiency;
        }

        private async Task UpdateTutorHashtagsAsync(string tutorId, UpdateTutorHashtagListRequest request)
        {
            var tutorHashtagRepo = _unitOfWork.GetRepository<TutorHashtag>();
            var existingHashtags = await tutorHashtagRepo.ExistEntities()
                .Where(th => th.TutorId == tutorId)
                .ToListAsync();

            var existingHashtagMap = existingHashtags.ToDictionary(h => h.HashtagId);
            var requestedHashtagSet = new HashSet<string>(request.HashtagIds.Distinct());

            var hashtagsToDelete = existingHashtags
                .Where(h => !requestedHashtagSet.Contains(h.HashtagId))
                .ToList();

            var hashtagsToAdd = request.ToTutorHashtagEntities(tutorId)
                .Where(ht => !existingHashtagMap.ContainsKey(ht.HashtagId))
                .ToList();

            if (hashtagsToDelete.Any())
                tutorHashtagRepo.DeleteRange(hashtagsToDelete, false);
            if (hashtagsToAdd.Any())
                tutorHashtagRepo.InsertRange(hashtagsToAdd);
        }

        public async Task<Dictionary<string, List<TutorCardDTO>>> GetTopTutorsByLanguagesAsync()
        {
            // Step 1: First get top 7 languages in a single query - push computation to database
            var popularLanguages = await _unitOfWork.GetRepository<TutorLanguage>()
                .ExistEntities()
                .GroupBy(tl => tl.LanguageCode)
                .Select(g => new { LanguageCode = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(7)
                .Select(x => x.LanguageCode)
                .ToListAsync();

            // Step 2: Get only tutors that teach these popular languages (with appropriate verification status)
            var relevantTutors = await _unitOfWork.GetRepository<Tutor>()
                .ExistEntities()
                .Where(t => t.VerificationStatus != VerificationStatus.Basic)
                .Include(t => t.User)
                .Where(t => _unitOfWork.GetRepository<TutorLanguage>()
                    .ExistEntities()
                    .Any(tl => tl.TutorId == t.UserId && popularLanguages.Contains(tl.LanguageCode)))
                .ToListAsync();

            var tutorIds = relevantTutors.Select(t => t.UserId).ToList();
            
            // Step 3: Get languages only for these tutors and only for popular languages
            var tutorLanguages = await _unitOfWork.GetRepository<TutorLanguage>()
                .ExistEntities()
                .Where(tl => tutorIds.Contains(tl.TutorId))
                .ToListAsync();

            // Build maps once to avoid redundant processing
            var tutorMap = relevantTutors.ToDictionary(t => t.UserId);
            var tutorLanguagesMap = tutorLanguages
                .GroupBy(tl => tl.TutorId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var result = new Dictionary<string, List<TutorCardDTO>>();

            // Process each language in parallel to improve performance
            Parallel.ForEach(popularLanguages, languageCode =>
            {
                var tutorIdsForLanguage = tutorLanguages
                    .Where(tl => tl.LanguageCode == languageCode)
                    .Select(tl => tl.TutorId)
                    .ToList();

                // Get tutors with proficiency info for the current language
                var languageTutorsWithProficiency = tutorIdsForLanguage
                    .Select(id => new {
                        Tutor = tutorMap[id],
                        Proficiency = tutorLanguages
                            .Where(tl => tl.TutorId == id && tl.LanguageCode == languageCode)
                            .Select(tl => tl.Proficiency)
                            .FirstOrDefault()
                    })
                    .ToList();

                // Get top 6 tutors by "rating"
                var topTutors = languageTutorsWithProficiency
                    .Select(x => {
                        var mockRating = GetMockRating(x.Tutor, x.Proficiency);
                        return new {
                            x.Tutor,
                            MockRating = mockRating
                        };
                    })
                    .OrderByDescending(x => x.MockRating)
                    .Take(6)
                    .Select(x => x.Tutor.ToTutorCardDTO(
                        tutorLanguagesMap.TryGetValue(x.Tutor.UserId, out var languages)
                            ? languages
                            : new List<TutorLanguage>(),
                        x.MockRating
                    ))
                    .ToList();

                lock(result)
                {
                    result[languageCode] = topTutors;
                }
            });

            return result;
        }

        // Remove this method when actual rating system is implemented
        private double GetMockRating(Tutor tutor, int languageProficiency)
        {
            // Mock rating formula:
            // - Base rating from 3.5 to 4.5
            // - Add bonus from verification status (0.0 to 0.5)
            // - Add bonus from language proficiency (0.0 to 0.5)
            
            // Generate a stable pseudo-random value based on tutor ID
            var random = new Random(tutor.UserId.GetHashCode());
            double baseRating = 3.5 + random.NextDouble();
            
            // Add bonus for verification status
            double verificationBonus = tutor.VerificationStatus == VerificationStatus.VerifiedHardcopy ? 0.5 : 0.0;
            
            // Add bonus for language proficiency
            double proficiencyBonus = languageProficiency / 14.0; // Max 0.5 for proficiency 7
            
            double finalRating = Math.Min(5.0, baseRating + verificationBonus + proficiencyBonus);
            return Math.Round(finalRating, 2);
        }
        #endregion

        public async Task<TutorResponse> RegisterAsTutorAsync(TutorRegistrationRequest request)
        {
            var userId = _userService.GetCurrentUserId();
            var appUser = await GetUserToCreateTutor(userId);

            var updatedUserFields = appUser.UpdateBasicInformation(
                request.FullName,
                request.DateOfBirth,
                request.Gender,
                request.Timezone);

            var newTutor = request.ToTutorProfile(appUser);

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (updatedUserFields.Length > 0)
                    _unitOfWork.GetRepository<AppUser>()
                        .UpdateFields(appUser, updatedUserFields);

                _unitOfWork.GetRepository<Tutor>().Insert(newTutor);

                await ProcessHashtags(userId, request.HashtagIds);
                await UpdateTutorLanguagesAsync(userId, request.Languages);

                var tutorApplication = TutorApplication.Create(userId);
                _unitOfWork.GetRepository<TutorApplication>().Insert(tutorApplication);

                await _unitOfWork.SaveAsync();
                await _userService.AddRoleToUserAsync(userId, Role.Tutor.ToStringRole());

                return newTutor.ToTutorResponse();
            }, 
            onError: ex => 
            {
                _logger.LogError(ex, "Tutor registration failed for user {UserId}", userId);
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    ErrorCode.ServerError,
                    "Tutor registration failed: " + ex.Message);
            });
        }

        public async Task<TutorResponse> SeedRegisterAsTutorAsync(string userId, TutorRegistrationRequest request)
        {
            // Get the user by ID directly instead of using current user
            var appUser = await _unitOfWork.GetRepository<AppUser>()
                .ExistEntities()
                .FirstOrDefaultAsync(u => u.Id == userId);
                
            if (appUser == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    $"User with ID {userId} not found.");
            
            // Check if tutor already exists
            var existingTutor = await _unitOfWork.GetRepository<Tutor>()
                .ExistEntities()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (existingTutor != null)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Người dùng đã đăng ký làm gia sư");

            var updatedUserFields = appUser.UpdateBasicInformation(
                request.FullName,
                request.DateOfBirth,
                request.Gender,
                request.Timezone);

            var newTutor = request.ToTutorProfile(appUser);

            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                if (updatedUserFields.Length > 0)
                    _unitOfWork.GetRepository<AppUser>()
                        .UpdateFields(appUser, updatedUserFields);

                _unitOfWork.GetRepository<Tutor>().Insert(newTutor);

                // Process hashtags
                if (request.HashtagIds != null && request.HashtagIds.Any())
                {
                    var tutorHashtags = request.HashtagIds.Select(hashtagId => new TutorHashtag 
                    {
                        TutorId = userId,
                        HashtagId = hashtagId
                    }).ToList();
                    
                    _unitOfWork.GetRepository<TutorHashtag>().InsertRange(tutorHashtags);
                }
                
                // Process languages
                if (request.Languages != null && request.Languages.Any())
                {
                    var tutorLanguages = request.Languages.Select(lang => new TutorLanguage
                    {
                        TutorId = userId,
                        LanguageCode = lang.LanguageCode,
                        Proficiency = lang.Proficiency,
                        IsPrimary = lang.IsPrimary
                    }).ToList();
                    
                    _unitOfWork.GetRepository<TutorLanguage>().InsertRange(tutorLanguages);
                }

                var tutorApplication = TutorApplication.Create(userId);
                _unitOfWork.GetRepository<TutorApplication>().Insert(tutorApplication);

                await _unitOfWork.SaveAsync();
                await _userService.AddRoleToUserAsync(userId, Role.Tutor.ToStringRole());

                return newTutor.ToTutorResponse();
            }, 
            onError: ex => 
            {
                _logger.LogError(ex, "Tutor registration failed for user {UserId}", userId);
                throw new ErrorException(
                    StatusCodes.Status500InternalServerError,
                    ErrorCode.ServerError,
                    "Tutor registration failed: " + ex.Message);
            });
        }

        public async Task UpdateLanguagesAsync(List<TutorLanguageDTO> languages)
        {
            var tutorId = _userService.GetCurrentUserId();
            await UpdateTutorLanguagesAsync(tutorId, languages);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateTutorHashtagsAsync(UpdateTutorHashtagListRequest request)
        {
            var tutorId = _userService.GetCurrentUserId();
            await UpdateTutorHashtagsAsync(tutorId, request);
            await _unitOfWork.SaveAsync();
        }

        public async Task<TutorResponse> GetByIdAsync(string id)
        {
            string trimmedId = id.Trim();
            
            var result = await _unitOfWork.ExecuteWithConnectionReuseAsync(async () => {
                // Get tutor with basic info
                var tutor = await _unitOfWork.GetRepository<Tutor>().ExistEntities()
                    .Include(t => t.User)
                    .Where(t => t.UserId == trimmedId)
                    .Select(TutorResponse.ProjectionExpression)
                    .FirstOrDefaultAsync();
                    
                if (tutor == null)
                    throw new ErrorException(
                        StatusCodes.Status404NotFound, 
                        ErrorCode.NotFound, 
                        $"Tutor with ID {id} not found.");

                // Get tutor hashtags
                var tutorHashtags = await _unitOfWork.GetRepository<TutorHashtag>().ExistEntities()
                    .Where(th => th.TutorId == trimmedId)
                    .Select(HashtagDTO.ProjectionExpression)
                    .ToListAsync();
                    
                // Get tutor languages
                var tutorLanguages = await _unitOfWork.GetRepository<TutorLanguage>().ExistEntities()
                    .Where(tl => tl.TutorId == trimmedId)
                    .Select(TutorLanguageDTO.ProjectionExpression)
                    .ToListAsync();
                
                // Get availability patterns using projection
                var patterns = await _unitOfWork.GetRepository<WeeklyAvailabilityPattern>().ExistEntities()
                    .Where(p => p.TutorId == trimmedId)
                    .OrderByDescending(p => p.AppliedFrom)
                    .Select(WeeklyAvailabilityDTO.ProjectionExpression)
                    .ToListAsync();
                    
                // Get booking slots using projection
                var bookings = await _unitOfWork.GetRepository<BookingSlot>().ExistEntities()
                    .Where(b => b.TutorId == trimmedId)
                    .Select(BookingSlotDTO.ProjectionExpression)
                    .ToListAsync();
                
                return (tutor, tutorHashtags, tutorLanguages, patterns, bookings);
            });
            
            var response = result.tutor;
            response.Hashtags = result.tutorHashtags;
            response.Languages = result.tutorLanguages;
            response.AvailabilityPatterns = result.patterns;
            response.BookingSlots = result.bookings;

            return response;
        }

        public async Task<VerificationStatus> GetVerificationStatusAsync(string id)
        {
            var tutorStatus = await _unitOfWork.GetRepository<Tutor>()
                .ExistEntities()
                .Where(t => t.UserId == id.Trim())
                .Select(t => (VerificationStatus?)t.VerificationStatus)
                .SingleOrDefaultAsync();

            if (tutorStatus == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    $"Tutor with ID {id} not found.");

            return tutorStatus.Value;
        }

        public async Task UpdateVerificationStatusAsync(
            string tutorId, 
            VerificationStatus status, 
            string? updatedBy = null)
        {
            var tutor = await GetTutorByIdAsync(tutorId);
            var modifiedProperties = tutor.UpdateVerificationStatus(status);
            
            if (modifiedProperties.Length > 0)
            {
                _unitOfWork.GetRepository<Tutor>().UpdateFields(tutor, modifiedProperties);
                await _unitOfWork.SaveAsync();
            }
        }

        public async Task<List<TutorHashtagDTO>> GetTutorHashtagsAsync()
        {
            var userId = _userService.GetCurrentUserId();
            var tutor = await GetTutorByIdAsync(userId);
            var tutorHashtags = await _unitOfWork.GetRepository<TutorHashtag>()
                .ExistEntities()
                .Where(th => th.TutorId == userId)
                .Include(th => th.Hashtag)
                .ToListAsync();

            return tutorHashtags.ToDTOs();
        }

        public async Task<List<TutorLanguageDTO>> GetTutorLanguagesAsync()
        {
            var userId = _userService.GetCurrentUserId();
            var tutor = await GetTutorByIdAsync(userId);
            var tutorLanguages = await _unitOfWork.GetRepository<TutorLanguage>()
                .ExistEntities()
                .Where(tl => tl.TutorId == userId)
                .ToListAsync();

            return tutorLanguages.ToDTOs();
        }

        public async Task<List<TutorCardDTO>> GetTutorCardListAsync()
        {
            var tutorsByLanguage = await GetTopTutorsByLanguagesAsync();
            
            return tutorsByLanguage.Values
                .SelectMany(tutors => tutors)
                .GroupBy(t => t.TutorId)
                .Select(g => g.First())
                .ToList();
        }

		public async Task<List<TutorCardDTO>> GetTutorCardsPagingAsync(int page =1, int size =20)
		{
			var tutors = await _unitOfWork.GetRepository<Tutor>().ExistEntities()
				.Include(t => t.User)
				.OrderByDescending(t => t.BecameTutorAt)
				.Skip((page-1) * size).Take(size)
				.ToListAsync();

			var tutorResponses = new List<TutorCardDTO>();
			foreach (var tutor in tutors)
			{
				tutorResponses.Add(tutor.ToTutorCardDTO(
					await _unitOfWork.GetRepository<TutorLanguage>().ExistEntities()
					.Where(t => t.TutorId.Equals(tutor.UserId))
					.ToListAsync(), 
					GetMockRating(tutor, 4)));	
			}	

			return tutorResponses;
		}
    }
}