using App.Core.Base;
using App.DTOs.AppUserDTOs.TutorDTOs;
using App.DTOs.BlogDTOs;
using App.DTOs.HashtagDTOs;
using App.Repositories.Models;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using App.Core.Constants;
using App.Repositories.Models.Papers;
using Microsoft.Extensions.Logging;
using App.Core.Provider;

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

                var tutorApplication = TutorApplication.Create(userId);
                _unitOfWork.GetRepository<TutorApplication>().Insert(tutorApplication);

                await _unitOfWork.SaveAsync();
                await _userService.AssignRoleAsync(userId, Role.Tutor.ToStringRole());

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
            var tutor = await GetTutorByIdAsync(id, includeUser: true);
            return tutor.ToTutorResponse();
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
    }
}