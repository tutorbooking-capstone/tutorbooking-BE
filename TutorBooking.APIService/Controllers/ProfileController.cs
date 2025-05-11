using App.Core.Base;
using App.DTOs.UserDTOs;
using App.Repositories.Models.User;
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

        public ProfileController(IProfileService profileService)
        {
            _profileService = profileService;
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

        [HttpPatch("fullname")]
        public async Task<IActionResult> UpdateFullName([FromBody] UpdateFullNameRequest request)
        {
            await _profileService.UpdateFullNameAsync(request.FullName);
            return Ok(new BaseResponseModel<string>(message: "Tên đã được cập nhật thành công."));
        }

        [HttpPatch("dateofbirth")]
        public async Task<IActionResult> UpdateDateOfBirth([FromBody] UpdateDateOfBirthRequest request)
        {
            await _profileService.UpdateDateOfBirthAsync(request.DateOfBirth);
            return Ok(new BaseResponseModel<string>(message: "Ngày sinh đã được cập nhật thành công."));
        }

        [HttpPatch("gender")]
        public async Task<IActionResult> UpdateGender([FromBody] UpdateGenderRequest request)
        {
            await _profileService.UpdateGenderAsync(request.Gender);
            return Ok(new BaseResponseModel<string>(message: "Giới tính đã được cập nhật thành công."));
        }

        [HttpPatch("")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            await _profileService.UpdateProfileAsync(request);
            return Ok(new BaseResponseModel<string>(
                message: "Thông tin cá nhân đã được cập nhật thành công."
            ));
        }
    }
}
