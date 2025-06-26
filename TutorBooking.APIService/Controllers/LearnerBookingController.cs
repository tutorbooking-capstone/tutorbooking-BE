using App.Core.Base;
using App.DTOs.BookingDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/learner-bookings")]
    [ApiController]
    [Authorize]
    public class LearnerBookingController : ControllerBase
    {
        private readonly ILearnerBookingService _service;

        public LearnerBookingController(ILearnerBookingService service)
        {
            _service = service;
        }

        [HttpPut("time-slots")]
        public async Task<IActionResult> UpdateTimeSlotRequests(
            [FromBody] LearnerTimeSlotRequestDTO request)
        {
            await _service.UpdateTimeSlotRequestsAsync(request);
            return Ok(new BaseResponseModel<object>(
                data: null,
                message: "Cập nhật yêu cầu khung giờ thành công!"
            ));
        }

        [HttpDelete("tutors/{tutorId}/time-slots")]
        public async Task<IActionResult> DeleteTimeSlotRequests(
            [FromRoute] string tutorId)
        {
            await _service.DeleteTimeSlotRequestsAsync(tutorId);
            return Ok(new BaseResponseModel<object>(
                data: null,
                message: "Xóa toàn bộ yêu cầu khung giờ thành công!"
            ));
        }

        [HttpGet("learners/{tutorId}/time-slots")]
        public async Task<IActionResult> GetTimeSlotRequestsByTutor(
            [FromRoute] string tutorId)
        {
            var requests = await _service.GetTimeSlotRequestsByTutorAsync(tutorId);
            return Ok(new BaseResponseModel<List<LearnerTimeSlotResponseDTO>>(
                data: requests,
                message: "Danh sách yêu cầu khung giờ"
            ));
        }
    }
}
