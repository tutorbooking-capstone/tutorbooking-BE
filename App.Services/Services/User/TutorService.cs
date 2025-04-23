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

namespace App.Services.Services.User
{
    public class TutorService : ITutorService
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;

        public TutorService(
            IUnitOfWork unitOfWork,
            IUserService userService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
        }
        #endregion

        #region Private Helper
        private async Task<AppUser> GetUserToCreateTutor(string userId)
        {
            var appUser = await _userService.GetCurrentUserAsync();

            //Check is Tutor exist with userId
            var tutorRepo = _unitOfWork.GetRepository<Tutor>();
            var existingTutor = await tutorRepo.ExistEntities()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (existingTutor != null)
                throw new ErrorException(
                    StatusCodes.Status400BadRequest,
                    ErrorCode.BadRequest,
                    "Người dùng này đã được đăng ký làm gia sư.");

            return appUser;
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
                tutorLanguageRepo.DeleteRange(languagesToDelete);

            var languagesToAdd = new List<TutorLanguage>();
            
            foreach (var requestedLang in requestedLanguages)
            {
                // Handle updates for existing languages
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
                tutorHashtagRepo.DeleteRange(hashtagsToDelete);
            if (hashtagsToAdd.Any())
                tutorHashtagRepo.InsertRange(hashtagsToAdd);
        }
        #endregion

        public async Task<TutorResponse> RegisterAsTutorAsync(TutorRegistrationRequest request)
        {
            var userId = _userService.GetCurrentUserId();
            var appUser = await GetUserToCreateTutor(userId);

            appUser.UpdateProfile(
                request.FullName, 
                request.PhoneNumber, 
                userId);

            var newTutor = appUser.BecameTutor(userId);
            _unitOfWork.GetRepository<Tutor>().Insert(newTutor);
            await _unitOfWork.SaveAsync();

            await _userService.AssignRoleAsync(userId, Role.Tutor.ToStringRole());

            return newTutor.ToTutorResponse();
        }

        public async Task UpdateLanguagesAsync(List<TutorLanguageDTO> languages)
        {
            var tutorId = _userService.GetCurrentUserId();
            await UpdateTutorLanguagesAsync(tutorId, languages);
            await _unitOfWork.SaveAsync();
        }

        public async Task UpdateHashtagsAsync(UpdateTutorHashtagListRequest request)
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

        public async Task UpdateVerificationStatusAsync(string tutorId, VerificationStatus status, string? updatedBy = null)
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