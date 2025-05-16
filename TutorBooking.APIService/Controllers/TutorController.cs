using App.Core.Base;
using App.DTOs.AppUserDTOs.TutorDTOs;
using App.DTOs.HashtagDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TutorController : ControllerBase
    {
        #region DI Constructor
        private readonly ITutorService _tutorService;

        public TutorController(ITutorService tutorService)
        {
            _tutorService = tutorService;
        }
        #endregion

        [HttpPost("register")]
        [Authorize]
        public async Task<IActionResult> RegisterAsTutor([FromBody] TutorRegistrationRequest request)
        {
            var tutor = await _tutorService.RegisterAsTutorAsync(request);
            return Ok(new BaseResponseModel<TutorResponse>(
                data: tutor,
                message: "Đăng ký làm gia sư thành công!"
            ));
        }

        //[AuthorizeRoles(Role.Tutor, Role.Admin)]
        [HttpPatch("update-languages")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> UpdateLanguages([FromBody] List<TutorLanguageDTO> languages)
        {
            await _tutorService.UpdateLanguagesAsync(languages);
            return Ok(new BaseResponseModel<string>(
                message: "Cập nhật ngôn ngữ thành công!"
            ));
        }

        [HttpPatch("update-tutor-hashtags")]
        [Authorize]
        public async Task<IActionResult> UpdateHashtags([FromBody] UpdateTutorHashtagListRequest request)
        {
            await _tutorService.UpdateTutorHashtagsAsync(request);
            return Ok(new BaseResponseModel<string>(
                message: "Cập nhật hashtag thành công!"
            ));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var tutor = await _tutorService.GetByIdAsync(id);
            return Ok(new BaseResponseModel<TutorResponse>(
                data: tutor
            ));
        }

        [HttpGet("{id}/verification-status")]
        public async Task<IActionResult> GetVerificationStatus(string id)
        {
            var status = await _tutorService.GetVerificationStatusAsync(id);
            return Ok(new BaseResponseModel<VerificationStatus>(
                data: status
            ));
        }

        [HttpPatch("{id}/verification-status")]
        public async Task<IActionResult> UpdateVerificationStatus(
            string tutorId, 
            [FromBody] VerificationStatus status,
            [FromQuery] string? updatedBy = null)
        {
            await _tutorService.UpdateVerificationStatusAsync(tutorId, status, updatedBy);
            return Ok(new BaseResponseModel<string>(
                message: "Cập nhật trạng thái xác minh thành công!"
            ));
        }

        [HttpGet("hashtags")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> GetTutorHashtags()
        {
            var hashtags = await _tutorService.GetTutorHashtagsAsync();
            return Ok(new BaseResponseModel<List<TutorHashtagDTO>>(
                data: hashtags,
                message: "Danh sách hashtag của gia sư."
            ));
        }

        [HttpGet("languages")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> GetTutorLanguages()
        {
            var languages = await _tutorService.GetTutorLanguagesAsync();
            return Ok(new BaseResponseModel<List<TutorLanguageDTO>>(
                data: languages,
                message: "Danh sách ngôn ngữ của gia sư."
            ));
        }

        [HttpGet("list-card")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTutorCardList()
        {
            var tutorCards = await _tutorService.GetTutorCardListAsync();
            return Ok(new BaseResponseModel<List<TutorCardDTO>>(
                data: tutorCards,
                message: "Danh sách gia sư."
            ));
        }
    }
}
