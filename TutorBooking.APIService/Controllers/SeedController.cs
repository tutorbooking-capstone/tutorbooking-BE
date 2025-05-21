using App.Core.Base;
using App.Repositories.Models;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    }
}
