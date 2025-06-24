using App.Core.Base;
using App.DTOs.LessonDTOs;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LessonController : ControllerBase
    {
        private readonly ILessonService _lessonService;

        public LessonController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        [HttpGet("tutor/{tutorId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLessonsByTutor(string tutorId)
        {
            var lessons = await _lessonService.GetAllLessonsByTutorAsync(tutorId);
            return Ok(new BaseResponseModel<List<LessonResponse>>(lessons, "Successfully retrieved lessons for tutor."));
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLessonById(string id)
        {
            var lesson = await _lessonService.GetLessonByIdAsync(id);
            return Ok(new BaseResponseModel<LessonResponse>(lesson, "Successfully retrieved lesson."));
        }

        [HttpPost]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> CreateLesson([FromBody] CreateLessonRequest request)
        {
            var newLesson = await _lessonService.CreateLessonAsync(request);
            return CreatedAtAction(nameof(GetLessonById), new { id = newLesson.Id }, new BaseResponseModel<LessonResponse>(newLesson, "Lesson created successfully."));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> UpdateLesson(string id, [FromBody] UpdateLessonRequest request)
        {
            var updatedLesson = await _lessonService.UpdateLessonAsync(id, request);
            return Ok(new BaseResponseModel<LessonResponse>(updatedLesson, "Lesson updated successfully."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> DeleteLesson(string id)
        {
            await _lessonService.DeleteLessonAsync(id);
            return Ok(new BaseResponseModel<object>(null, "Lesson deleted successfully."));
        }
    }
}
