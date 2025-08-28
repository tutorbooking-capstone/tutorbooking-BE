using App.Core.Base;
using App.DTOs.AppUserDTOs.TutorDTOs;
using App.Repositories.Models;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TutorIntroductionVideoController : ControllerBase
    {
        private readonly ITutorIntroductionVideoService _tutorIntroductionVideoService;

        public TutorIntroductionVideoController(ITutorIntroductionVideoService tutorIntroductionVideoService)
        {
            _tutorIntroductionVideoService = tutorIntroductionVideoService;
        }

        [HttpPost]
        [AuthorizeRoles(Role.Learner, Role.Tutor)]
        public async Task<IActionResult> CreateTutorIntroductionVideo([FromBody] TutorIntroductionVideoRequest request)
        {
            var video = await _tutorIntroductionVideoService.CreateAsync(request);
            return Ok(new BaseResponseModel<TutorIntroductionVideoResponse>(video, "SUCCESS"));
        }

        [HttpPatch("set-active")]
        [AuthorizeRoles(Role.Learner, Role.Tutor)]
        public async Task<IActionResult> SetActiveTutorIntroductionVideo([FromBody]TutorIntroductionVideoStatusUpdateRequest request)
        {
            await _tutorIntroductionVideoService.UpdateStatusAsync(request);
            return Ok(new BaseResponseModel<object>(null, "SUCCESS"));
        }

        [HttpDelete("{id}")]
        [AuthorizeRoles(Role.Learner, Role.Tutor)]
        public async Task<IActionResult> DeleteTutorIntroductionVideo(string id)
        {
            await _tutorIntroductionVideoService.DeleteAsync(id);
            return Ok(new BaseResponseModel<object>(null, "SUCCESS"));
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTutorIntroductionVideoById(string id)
        {
            var video = await _tutorIntroductionVideoService.GetByIdAsync(id);
            return Ok(new BaseResponseModel<TutorIntroductionVideoResponse?>(video, "SUCCESS"));
        }

        [HttpGet]
        [Authorize]// TODO: Staff Role 
        public async Task<IActionResult> GetTutorIntroductionVideos(
            TutorIntroductionVideoStatus? status, 
            string? tutorId, 
            int page = 1, 
            int size = 20)
        {
            var videos = await _tutorIntroductionVideoService.GetAsync(status, tutorId, page, size);
            return Ok(new BaseResponseModel<object>(videos.Items, videos.AdditionalData, "SUCCESS"));
        }

        [HttpGet("current-user")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUserTutorIntroductionVideos(
            TutorIntroductionVideoStatus? status, 
            int page = 1, 
            int size = 10)
        {
            var videos = await _tutorIntroductionVideoService.GetByCurrentUserIdAsync(status, page, size);
            return Ok(new BaseResponseModel<object>(videos.Items, videos.AdditionalData, "SUCCESS"));
        }

        [HttpPost("review")]
        [AuthorizeRoles(Role.Staff)]
        public async Task<IActionResult> ReviewTutorIntroductionVideo([FromBody] TutorIntroductionVideoReviewRequest request)
        {
            var video = await _tutorIntroductionVideoService.ReviewAsync(request);
            return Ok(new BaseResponseModel<TutorIntroductionVideoResponse>(video, "SUCCESS"));
        }
    }
}
