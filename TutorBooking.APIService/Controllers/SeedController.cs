using App.Core.Base;
using App.Repositories.Models;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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


    }
}
