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

        [HttpDelete("weekly-pattern/{patternId}")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> DeleteWeeklyPattern(string patternId)
        {
            await _scheduleService.DeleteWeeklyPatternAsync(patternId);
            return Ok(new BaseResponseModel<object>(
                data: null, 
                message: "Xóa lịch tuần thành công!"));
        }

        [HttpGet("tutors/{tutorId}/weekly-patterns")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllWeeklyPatterns([FromRoute] string tutorId)
        {
            var patterns = await _scheduleService.GetAllWeeklyPatternsAsync(tutorId);
            return Ok(new BaseResponseModel<List<WeeklyPatternResponse>>(
                data: patterns,
                message: "Danh sách lịch tuần của gia sư"
            ));
        }

        [HttpGet("tutors/{tutorId}/week")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWeekAvailability(
            [FromRoute] string tutorId,
            [FromQuery] DateTime startDate)
        {
            var availability = await _scheduleService.GetWeekAvailabilityAsync(tutorId, startDate);
            return Ok(new BaseResponseModel<List<DailyAvailabilityPatternDTO>>(
                data: availability,
                message: "Lịch rảnh dự kiến của gia sư trong 7 ngày"
            ));
        }
    }
}
