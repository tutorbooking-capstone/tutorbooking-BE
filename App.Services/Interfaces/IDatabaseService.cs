using App.Repositories.Models;
using App.Repositories.Models.Papers;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using Microsoft.AspNetCore.Identity;

namespace App.Services.Interfaces
{
    public interface IDatabaseService
    {
        #region AppUser Related Tables
        Task<List<AppUser>> GetAllUsersAsync();
        Task<List<IdentityRole>> GetAllRolesAsync();
        #endregion

        #region User Related Tables
        Task<List<Tutor>> GetAllTutorsAsync();
        Task<List<Learner>> GetAllLearnersAsync();
        Task<List<Staff>> GetAllStaffsAsync();
        #endregion

        #region Application Related Tables
        Task<List<TutorApplication>> GetAllTutorApplicationsAsync();
        Task<List<ApplicationRevision>> GetAllApplicationRevisionsAsync();
        Task<List<HardcopySubmit>> GetAllHardcopySubmitsAsync();
        #endregion

        #region Tutor Related Tables
        Task<List<TutorLanguage>> GetAllTutorLanguagesAsync();
        Task<List<TutorHashtag>> GetAllTutorHashtagsAsync();
        #endregion

        #region Document Related Tables
        Task<List<Document>> GetAllDocumentsAsync();
        Task<List<DocumentFileUpload>> GetAllDocumentFileUploadsAsync();
        #endregion

        #region Scheduling Related Tables
        Task<List<WeeklyAvailabilityPattern>> GetAllWeeklyAvailabilityPatternsAsync();
        Task<List<BookingSlot>> GetAllBookingSlotsAsync();
        Task<List<AvailabilitySlot>> GetAllAvailabilitySlotsAsync();
        #endregion

        #region Other Tables
        Task<List<Blog>> GetAllBlogsAsync();
        Task<List<Hashtag>> GetAllHashtagsAsync();
        #endregion
    }
}
