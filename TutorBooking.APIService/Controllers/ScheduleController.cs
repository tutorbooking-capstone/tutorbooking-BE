using App.Core.Base;
using App.DTOs.ScheduleDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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
        [AllowAnonymous]
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

        [HttpPut("weekly-pattern")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> UpdateWeeklyPattern([FromBody] UpdateWeeklyPatternRequest request)
        {
            var response = await _scheduleService.UpdateWeeklyPatternAsync(request);
            return Ok(new BaseResponseModel<WeeklyPatternResponse>(
                data: response, 
                message: "Cập nhật lịch rãnh thành công!"));
        }
    }
}
