using App.Core.Base;
using App.DTOs.UserDTOs;
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
            if (file == null || file.Length == 0)
                return BadRequest(new BaseResponseModel<object>(message: "Vui lòng chọn một tệp ảnh."));

            if (!file.ContentType.StartsWith("image/"))
                return BadRequest(new BaseResponseModel<object>(message: "Tệp tải lên không phải là ảnh hợp lệ."));

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
    }
}
