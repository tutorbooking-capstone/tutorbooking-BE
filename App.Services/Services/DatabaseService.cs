using App.Core.Base;
using App.Repositories.Context;
using App.Repositories.Models;
using App.Repositories.Models.Papers;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services
{
    internal class DatabaseService : IDatabaseService
    {
        #region DI Constructor
        private readonly AppDbContext _dbContext;

        public DatabaseService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        #endregion

        #region AppUser Related Tables
        public async Task<List<AppUser>> GetAllUsersAsync()
            => await _dbContext.Users.ToListAsync();

        public async Task<List<IdentityRole>> GetAllRolesAsync()
            => await _dbContext.Roles.ToListAsync();
        #endregion

        #region User Related Tables
        public async Task<List<Tutor>> GetAllTutorsAsync()
            => await _dbContext.Tutors.ToListAsync();

        public async Task<List<Learner>> GetAllLearnersAsync()
            => await _dbContext.Learners.ToListAsync();

        public async Task<List<Staff>> GetAllStaffsAsync()
            => await _dbContext.Staffs.ToListAsync();
        #endregion

        #region Application Related Tables
        public async Task<List<TutorApplication>> GetAllTutorApplicationsAsync()
            => await _dbContext.TutorApplications.ToListAsync();

        public async Task<List<ApplicationRevision>> GetAllApplicationRevisionsAsync()
            => await _dbContext.ApplicationRevisions.ToListAsync();

        public async Task<List<HardcopySubmit>> GetAllHardcopySubmitsAsync()
            => await _dbContext.HardcopySubmits.ToListAsync();
        #endregion

        #region Tutor Related Tables
        public async Task<List<TutorLanguage>> GetAllTutorLanguagesAsync()
            => await _dbContext.TutorLanguages.ToListAsync();

        public async Task<List<TutorHashtag>> GetAllTutorHashtagsAsync()
            => await _dbContext.TutorHashtags.ToListAsync();
        #endregion

        #region Document Related Tables
        public async Task<List<Document>> GetAllDocumentsAsync()
            => await _dbContext.Documents.ToListAsync();

        public async Task<List<DocumentFileUpload>> GetAllDocumentFileUploadsAsync()
            => await _dbContext.DocumentFileUploads.ToListAsync();
        #endregion

        #region Scheduling Related Tables
        public async Task<List<WeeklyAvailabilityPattern>> GetAllWeeklyAvailabilityPatternsAsync()
            => await _dbContext.WeeklyAvailabilityPatterns.ToListAsync();

        public async Task<List<BookingSlot>> GetAllBookingSlotsAsync()
            => await _dbContext.BookingSlots.ToListAsync();

        public async Task<List<AvailabilitySlot>> GetAllAvailabilitySlotsAsync()
            => await _dbContext.AvailabilitySlots.ToListAsync();
        #endregion

        #region Other Tables
        public async Task<List<Blog>> GetAllBlogsAsync()
            => await _dbContext.Blogs.ToListAsync();

        public async Task<List<Hashtag>> GetAllHashtagsAsync()
            => await _dbContext.Hashtags.ToListAsync();
        #endregion
    }
}
