using App.Core.Base;
using App.DTOs.AppUserDTOs.LearnerDTOs;
using App.DTOs.AppUserDTOs.TutorDTOs;
using App.DTOs.UserDTOs;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/profile")]
    [ApiController]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        #region DI Constructor
        private readonly IProfileService _profileService;
        private readonly ILearnerService _learnerService;

        public ProfileController(
            IProfileService profileService,
            ILearnerService learnerService)
        {
            _profileService = profileService;
            _learnerService = learnerService;
        }
        #endregion

        [HttpPost("image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            var result = await _profileService.UploadProfileImageAsync(file);
            return Ok(new BaseResponseModel<ProfileImageResponseDTO>(
                data: result,
                message: "Ảnh đại diện đã được cập nhật thành công."
            ));
        }

        [HttpDelete("image")]
        public async Task<IActionResult> DeleteProfileImage()
        {
            await _profileService.DeleteProfileImageAsync();
            return Ok(new BaseResponseModel<string>(
                message: "Ảnh đại diện đã được xóa thành công."
            ));
        }

        [HttpPatch("user")]
        public async Task<IActionResult> UpdateBasicInformation([FromBody] UpdateBasicInformationRequest request)
        {
            await _profileService.UpdateBasicInformationAsync(request);
            return Ok(new BaseResponseModel<string>(
                message: "Thông tin cá nhân đã được cập nhật thành công."
            ));
        }

        [HttpGet("user")]
        public async Task<IActionResult> GetUserProfile()
        {
            var profileData = await _profileService.GetUserProfileAsync();
            return Ok(new BaseResponseModel<UserProfileResponse>(
                data: profileData,
                message: "Thông tin hồ sơ người dùng."
            ));
        }

        [HttpGet("tutor-register-profile")]
        public async Task<IActionResult> GetTutorRegistrationProfile()
        {
            var profileData = await _profileService.GetTutorRegistrationProfileAsync();
            return Ok(new BaseResponseModel<TutorRegistrationProfileResponse>(
                data: profileData,
                message: "Thông tin hồ sơ đăng ký gia sư."
            ));
        }

        [HttpPatch("language")]
        public async Task<IActionResult> UpdateLearningLanguage([FromBody] UpdateLearnerLanguageRequest request)
        {
            await _learnerService.UpdateLearningLanguageAsync(request);
            return Ok(new BaseResponseModel<string>(
                message: "Ngôn ngữ học tập đã được cập nhật thành công."
            ));
        }

        [HttpGet("language")]
        public async Task<IActionResult> GetLearningLanguage()
        {
            var (languageCode, proficiencyLevel) = await _learnerService.GetLearningLanguageAsync();
            return Ok(new BaseResponseModel<object>(
                data: new { LanguageCode = languageCode, ProficiencyLevel = proficiencyLevel },
                message: "Thông tin ngôn ngữ học tập.",
                additionalData: "For Dev only: Dùng mã ISO, kham khảo tại: https://www.w3schools.com/tags/ref_language_codes.asp"
            ));
        }
    }
}

#region Part Endpoint
//[HttpPatch("fullname")]
//public async Task<IActionResult> UpdateFullName([FromBody] UpdateFullNameRequest request)
//{
//    await _profileService.UpdateFullNameAsync(request.FullName);
//    return Ok(new BaseResponseModel<string>(message: "Tên đã được cập nhật thành công."));
//}

//[HttpPatch("dateofbirth")]
//public async Task<IActionResult> UpdateDateOfBirth([FromBody] UpdateDateOfBirthRequest request)
//{
//    await _profileService.UpdateDateOfBirthAsync(request.DateOfBirth);
//    return Ok(new BaseResponseModel<string>(message: "Ngày sinh đã được cập nhật thành công."));
//}

//[HttpPatch("gender")]
//public async Task<IActionResult> UpdateGender([FromBody] UpdateGenderRequest request)
//{
//    await _profileService.UpdateGenderAsync(request.Gender);
//    return Ok(new BaseResponseModel<string>(message: "Giới tính đã được cập nhật thành công."));
//}
#endregion