using App.Core.Base;
using App.DTOs.ApplicationDTOs.TutorApplicationDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using App.Services.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/tutorapplication/staff")]
    [ApiController]
    public class TutorApplicationStaffController : ControllerBase
    {
        private ITutorApplicationStaffService _tutorApplicationStaffService;

        public TutorApplicationStaffController(ITutorApplicationStaffService tutorApplicationStaffService)
        {
            _tutorApplicationStaffService = tutorApplicationStaffService;
        }

        [HttpGet("pending-applications")]
        [Authorize]
        public async Task<IActionResult> GetAllPendingApplications(int page =1, int size =20)
        {
            return Ok(new BaseResponseModel<object>(
                data: await _tutorApplicationStaffService.GetAllPendingTutorApplicationsAsync(page, size),
                message: "SUCCESS"
                ));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetApplicationById(string id)
        {
            return Ok(new BaseResponseModel<object>(
                data: await _tutorApplicationStaffService.GetTutorApplicationByIdAsync(id),
                message: "SUCCESS"
                ));
        }

        [HttpPost("review")]
        [Authorize]
        public async Task<IActionResult> CreateApplicationRevision(ApplicationRevisionCreateRequest request)
        {
            return Ok(new BaseResponseModel<object>(
                data: await _tutorApplicationStaffService.CreateApplicationRevisionAsync(request),
                message: "SUCCESS"
                ));
        }
    }
}
