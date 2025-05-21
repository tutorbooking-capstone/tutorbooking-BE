using App.Core.Base;
using App.Repositories.Models;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using App.DTOs.AuthDTOs;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class SeedController : ControllerBase
    {
        #region DI Constructor
        private readonly ISeedService _seedService;

        public SeedController(ISeedService seedService)
        {
            _seedService = seedService;
        }
        #endregion

        [HttpPost("hashtags")]
        public async Task<IActionResult> SeedHashtags()
        {
            var hashtags = await _seedService.SeedHashtagsAsync();
            return Ok(new BaseResponseModel<List<Hashtag>>(
                data: hashtags,
                message: "Seed hashtags thành công!"
            ));
        }

        [HttpPost("tutor/availability/{tutorId}")]
        public async Task<IActionResult> SeedTutorAvailability(string tutorId)
        {
            var pattern = await _seedService.SeedTutorAvailabilityAsync(tutorId);
            return Ok(new BaseResponseModel<string>(
                data: pattern.Id, 
                message: $"Seed lịch sẵn có cho gia sư {tutorId} thành công!"
            ));
        }

        [HttpPost("tutor/bookings/{tutorId}")]
        public async Task<IActionResult> SeedTutorBookings(
            string tutorId, 
            [FromBody] List<string> learnerIds,
            [FromQuery] int count = 3)
        {
            var bookings = await _seedService.SeedTutorBookingsAsync(tutorId, learnerIds, count);
            return Ok(new BaseResponseModel<int>(
                data: bookings.Count, 
                message: $"Seed {bookings.Count} lịch đặt cho gia sư {tutorId} thành công!"
            ));
        }

        [HttpPost("users")]
        public async Task<IActionResult> SeedUsers(
            [FromQuery] string prefix,
            [FromQuery] int count = 10)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return BadRequest(new BaseResponseModel<string>(
                    message: "Email prefix is required"
                ));

            if (count <= 0)
                return BadRequest(new BaseResponseModel<string>(
                    message: "Count must be greater than 0"
                ));

            var emails = await _seedService.SeedUsersAsync(prefix, count);
            
            return Ok(new BaseResponseModel<List<string>>(
                data: emails,
                message: $"Seed {emails.Count} user accounts with prefix '{prefix}' successfully!"
            ));
        }

        [HttpPost("tutors")]
        public async Task<IActionResult> SeedTutors(
            [FromQuery] string prefix,
            [FromQuery] int count = 5)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return BadRequest(new BaseResponseModel<string>(
                    message: "Email prefix is required"
                ));

            if (count <= 0)
                return BadRequest(new BaseResponseModel<string>(
                    message: "Count must be greater than 0"
                ));

            var tutorEmails = await _seedService.SeedTutorsAsync(prefix, count);
            
            return Ok(new BaseResponseModel<List<string>>(
                data: tutorEmails,
                message: $"Successfully registered {tutorEmails.Count} users as tutors with prefix '{prefix}'!"
            ));
        }

        [HttpPost("tutor-details")]
        public async Task<IActionResult> SeedAllTutorDetails(
            [FromQuery] string tutorPrefix = "tutor",
            [FromQuery] string learnerPrefix = "learner",
            [FromQuery] int tutorCount = 70,
            [FromQuery] int learnerCount = 70)
        {
            var processedCount = await _seedService.SeedAllTutorDetailsAsync(
                tutorPrefix, learnerPrefix, tutorCount, learnerCount);
            
            return Ok(new BaseResponseModel<int>(
                data: processedCount,
                message: $"Successfully seeded details for {processedCount} tutors!"
            ));
        }
    }
}
