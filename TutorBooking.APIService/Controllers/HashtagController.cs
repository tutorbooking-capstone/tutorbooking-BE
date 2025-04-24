using App.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using App.Core.Base;
using App.Repositories.Models;

namespace TutorBooking.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HashtagController : ControllerBase
    {
        #region DI Constructor
        private readonly IHashtagService _hashtagService;

        public HashtagController(IHashtagService hashtagService)
        {
            _hashtagService = hashtagService;
        }
        #endregion

        // [HttpPost("seed")]
        // public async Task<IActionResult> SeedHashtags()
        // {
        //     await _hashtagService.SeedHashtagsAsync();
        //     return Ok(new BaseResponseModel<string>("Seeding hashtags thành công!"));
        // }

        // [HttpGet("get-seed")]
        // public IActionResult GetSeededHashtags() 
        // {
        //     var hashtags = _hashtagService.GetSeedHashtags();
        //     return Ok(new BaseResponseModel<List<Hashtag>>(hashtags));
        // }

    }
}