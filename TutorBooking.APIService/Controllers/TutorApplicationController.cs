using App.Core.Base;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TutorApplicationController : ControllerBase
    {
        private ITutorApplicationService _service;

        public TutorApplicationController(ITutorApplicationService service)
        {
            _service = service;
        }

        [HttpPost]
        [AuthorizeRoles(Role.Learner)]
        public async Task<IActionResult> CreateApplication(string tutorId)
        {
            await _service.CreateTutorApplicationAsync(tutorId);
            return Ok(new BaseResponseModel<object>(
                message: "SUCCESS"
                ));
        }

        [HttpPost("request-verification")]
        [AuthorizeRoles(Role.Learner)]
        public async Task<IActionResult> RequestVerification(string tutorApplicationId)
        {
            await _service.RequestVerificationAsync(tutorApplicationId);
            return Ok(new BaseResponseModel<object>(
                message: "SUCCESS"
                ));
        }

        [HttpGet("metadata")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMetadata()
        {
            var metadata = await _service.GetApplicationMetadataAsync();
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Metadata cho quy trình xác minh gia sư"
            ));
        }
    }
}
