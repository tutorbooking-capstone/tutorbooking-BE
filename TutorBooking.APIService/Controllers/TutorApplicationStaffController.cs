using App.Core.Base;
using App.DTOs.ApplicationDTOs.TutorApplicationDTOs;
using App.Repositories.Models.Papers;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TutorBooking.APIService.EventHandlers;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/tutorapplication/staff")]
    [ApiController]
    public class TutorApplicationStaffController : ControllerBase
    {
        private ITutorApplicationStaffService _tutorApplicationStaffService;
        private PushNotificationEventHandler _notificationEventHandler;

        public TutorApplicationStaffController(ITutorApplicationStaffService tutorApplicationStaffService, PushNotificationEventHandler notificationEventHandler)
        {
            _tutorApplicationStaffService = tutorApplicationStaffService;
            _notificationEventHandler = notificationEventHandler;
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

        [Authorize]
        [HttpGet("applications")]
        public async Task<IActionResult> GetApplications(
            [FromQuery] ApplicationStatus? status,
            [FromQuery] int page = 1, 
            [FromQuery] int size = 20)
        {

            var result = await _tutorApplicationStaffService.GetAllTutorApplicationsAsync(status, page, size);

            return Ok(new BaseResponseModel<object>(
                data: result.Items,
                additionalData: result.AdditionalData,
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

        [HttpGet("metadata")]
        [Authorize]
        public async Task<IActionResult> GetMetadata()
        {
            var metadata = await _tutorApplicationStaffService.GetApplicationMetadataAsync();
            return Ok(new BaseResponseModel<object>(
                data: metadata,
                message: "Metadata cho quy trình xác minh gia sư"
            ));
        }
    }
}
