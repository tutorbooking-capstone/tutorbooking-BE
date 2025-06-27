using App.Core.Base;
using App.Services.Interfaces;
using App.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
        [Authorize]
        public async Task<IActionResult> CreateApplication(string tutorId)
        {
            await _service.CreateTutorApplicationAsync(tutorId);
            return Ok(new BaseResponseModel<object>(
                message: "SUCCESS"
                ));
        }

        [HttpPost("request-verification")]
        [Authorize]
        public async Task<IActionResult> RequestVerification(string tutorApplicationId)
        {
            await _service.RequestVerificationAsync(tutorApplicationId);
            return Ok(new BaseResponseModel<object>(
                message: "SUCCESS"
                ));
        }
    }
}
