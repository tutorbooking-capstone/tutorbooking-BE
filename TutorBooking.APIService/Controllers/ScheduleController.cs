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

        [HttpPost("weekly-pattern/create")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> CreateWeeklyPattern([FromBody] CreateWeeklyPatternRequest request)
        {
            var response = await _scheduleService.CreateWeeklyPatternAsync(request);
            return Ok(new BaseResponseModel<WeeklyPatternResponse>(
                data: response, 
                message: "Tạo lịch tuần mới thành công!"));
        }

        [HttpGet("weekly-pattern/detail/{patternId}")]
        [AuthorizeRoles(Role.Tutor)]
        public async Task<IActionResult> GetWeeklyPatternDetail([FromRoute] string patternId)
        {
            var pattern = await _scheduleService.GetWeeklyPatternDetailAsync(patternId);
            return Ok(new BaseResponseModel<WeeklyPatternDetailResponse>(
                data: pattern,
                message: "Chi tiết lịch tuần"));
        }

        [HttpGet("tutors/{tutorId}/list-weekly-patterns")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWeeklyPatternsWithDates([FromRoute] string tutorId)
        {
            var patterns = await _scheduleService.GetWeeklyPatternsWithDatesAsync(tutorId);
            return Ok(new BaseResponseModel<List<WeeklyPatternWithDatesResponse>>(
                data: patterns,
                message: "Danh sách lịch tuần của gia sư kèm thời hạn"));
        }

        [HttpGet("tutors/{tutorId}/schedule")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTutorSchedule(
            [FromRoute] string tutorId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var schedule = await _scheduleService.GetTutorScheduleAsync(tutorId, startDate, endDate);
            return Ok(new BaseResponseModel<List<DailyScheduleResponse>>(
                data: schedule,
                message: "Lịch trình của gia sư"
            ));
        }
    }
}
