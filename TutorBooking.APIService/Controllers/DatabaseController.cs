//using App.Core.Base;
//using App.Repositories.Models;
//using App.Repositories.Models.Papers;
//using App.Repositories.Models.Scheduling;
//using App.Repositories.Models.User;
//using App.Services.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;

//namespace TutorBooking.APIService.Controllers
//{
//    [Route("api/database")]
//    [ApiController]
//    [AllowAnonymous]
//    public class DatabaseController : ControllerBase
//    {
//        #region DI Constructor
//        private readonly IDatabaseService _databaseService;

//        public DatabaseController(IDatabaseService databaseService)
//        {
//            _databaseService = databaseService;
//        }
//        #endregion

//        #region AppUser Related Endpoints
//        [HttpGet("users")]
//        public async Task<IActionResult> GetAllUsers()
//            => Ok(new BaseResponseModel<List<AppUser>>(await _databaseService.GetAllUsersAsync()));

//        [HttpGet("roles")]
//        public async Task<IActionResult> GetAllRoles()
//            => Ok(new BaseResponseModel<List<IdentityRole>>(await _databaseService.GetAllRolesAsync()));
//        #endregion


//        #region User Related Endpoints
//        [HttpGet("tutors")]
//        public async Task<IActionResult> GetAllTutors()
//            => Ok(new BaseResponseModel<List<Tutor>>(await _databaseService.GetAllTutorsAsync()));

//        [HttpGet("learners")]
//        public async Task<IActionResult> GetAllLearners()
//            => Ok(new BaseResponseModel<List<Learner>>(await _databaseService.GetAllLearnersAsync()));

//        [HttpGet("staffs")]
//        public async Task<IActionResult> GetAllStaffs()
//            => Ok(new BaseResponseModel<List<Staff>>(await _databaseService.GetAllStaffsAsync()));
//        #endregion

//        #region Application Related Endpoints
//        [HttpGet("tutor-applications")]
//        public async Task<IActionResult> GetAllTutorApplications()
//            => Ok(new BaseResponseModel<List<TutorApplication>>(await _databaseService.GetAllTutorApplicationsAsync()));

//        [HttpGet("application-revisions")]
//        public async Task<IActionResult> GetAllApplicationRevisions()
//            => Ok(new BaseResponseModel<List<ApplicationRevision>>(await _databaseService.GetAllApplicationRevisionsAsync()));

//        [HttpGet("hardcopy-submits")]
//        public async Task<IActionResult> GetAllHardcopySubmits()
//            => Ok(new BaseResponseModel<List<HardcopySubmit>>(await _databaseService.GetAllHardcopySubmitsAsync()));
//        #endregion

//        #region Tutor Related Endpoints
//        [HttpGet("tutor-languages")]
//        public async Task<IActionResult> GetAllTutorLanguages()
//            => Ok(new BaseResponseModel<List<TutorLanguage>>(await _databaseService.GetAllTutorLanguagesAsync()));

//        [HttpGet("tutor-hashtags")]
//        public async Task<IActionResult> GetAllTutorHashtags()
//            => Ok(new BaseResponseModel<List<TutorHashtag>>(await _databaseService.GetAllTutorHashtagsAsync()));
//        #endregion

//        #region Document Related Endpoints
//        [HttpGet("documents")]
//        public async Task<IActionResult> GetAllDocuments()
//            => Ok(new BaseResponseModel<List<Document>>(await _databaseService.GetAllDocumentsAsync()));

//        [HttpGet("document-file-uploads")]
//        public async Task<IActionResult> GetAllDocumentFileUploads()
//            => Ok(new BaseResponseModel<List<DocumentFileUpload>>(await _databaseService.GetAllDocumentFileUploadsAsync()));
//        #endregion

//        #region Scheduling Related Endpoints
//        [HttpGet("weekly-availability-patterns")]
//        public async Task<IActionResult> GetAllWeeklyAvailabilityPatterns()
//            => Ok(new BaseResponseModel<List<WeeklyAvailabilityPattern>>(await _databaseService.GetAllWeeklyAvailabilityPatternsAsync()));

//        [HttpGet("booking-slots")]
//        public async Task<IActionResult> GetAllBookingSlots()
//            => Ok(new BaseResponseModel<List<BookingSlot>>(await _databaseService.GetAllBookingSlotsAsync()));

//        [HttpGet("availability-slots")]
//        public async Task<IActionResult> GetAllAvailabilitySlots()
//            => Ok(new BaseResponseModel<List<AvailabilitySlot>>(await _databaseService.GetAllAvailabilitySlotsAsync()));
//        #endregion

//        #region Other Endpoints
//        [HttpGet("blogs")]
//        public async Task<IActionResult> GetAllBlogs()
//            => Ok(new BaseResponseModel<List<Blog>>(await _databaseService.GetAllBlogsAsync()));

//        [HttpGet("hashtags")]
//        public async Task<IActionResult> GetAllHashtags()
//            => Ok(new BaseResponseModel<List<Hashtag>>(await _databaseService.GetAllHashtagsAsync()));
//        #endregion
//    }
//}
