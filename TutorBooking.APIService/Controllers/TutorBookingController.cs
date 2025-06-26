using App.Core.Base;
using App.DTOs.BookingDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/tutor-bookings")]
    [ApiController]
    [Authorize]
    public class TutorBookingController : ControllerBase
    {
        private readonly ITutorBookingService _service;

        public TutorBookingController(ITutorBookingService service)
        {
            _service = service;
        }

        [HttpGet("time-slots")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> GetAllTimeSlotRequestsForTutor()
        {
            var requests = await _service.GetAllTimeSlotRequestsForTutorAsync();
            return Ok(new BaseResponseModel<List<LearnerInfoDTO>>(
                data: requests,
                message: "Danh sách học viên đã gửi yêu cầu"
            ));
        }

        [HttpGet("{learnerId}/time-slots")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> GetTimeSlotRequestsByLearner(
            [FromRoute] string learnerId)
        {
            var requests = await _service.GetTimeSlotRequestsByLearnerAsync(learnerId);
            return Ok(new BaseResponseModel<List<LearnerTimeSlotResponseDTO>>(
                data: requests,
                message: "Danh sách yêu cầu khung giờ từ học viên"
            ));
        }
    }
}