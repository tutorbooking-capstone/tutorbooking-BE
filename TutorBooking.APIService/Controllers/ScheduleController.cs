using App.Core.Base;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/schedule")]
    [ApiController]
    [AllowAnonymous]
    public class ScheduleController : ControllerBase
    {
        #region DI Constructor
        private readonly IScheduleService _scheduleService;

        public ScheduleController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }
        #endregion

        [HttpGet("tutors/{tutorId}/availability")]
        public async Task<IActionResult> GetTutorAvailability(
            [FromRoute] string tutorId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var availability = await _scheduleService.GetTutorAvailabilityAsync(tutorId, startDate, endDate);
            return Ok(new BaseResponseModel<List<DailyAvailabilityDTO>>(
                data: availability,
                message: "Lịch của gia sư"
            ));
        }
    }
}
