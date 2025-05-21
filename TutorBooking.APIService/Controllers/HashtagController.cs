using App.Core.Base;
using App.DTOs.HashtagDTOs;
using App.Repositories.Models;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class HashtagController : ControllerBase
    {
        #region DI Constructor
        private readonly IHashtagService _hashtagService;

        public HashtagController(IHashtagService hashtagService)
        {
            _hashtagService = hashtagService;
        }
        #endregion

        //#region Seed Hashtags
        //[HttpPost("seed")]
        //public async Task<IActionResult> SeedHashtags()
        //{
        //    await _hashtagService.SeedHashtagsAsync();
        //    return Ok(new BaseResponseModel<string>("Seeding hashtags thành công!"));
        //}

        //[HttpGet("get-seed")]
        //public IActionResult GetSeededHashtags()
        //{
        //    var hashtags = _hashtagService.GetSeedHashtags();
        //    return Ok(new BaseResponseModel<List<Hashtag>>(hashtags));
        //}
        //#endregion

        [HttpGet("all")]
        public async Task<IActionResult> GetAllHashtags()
        {
            var hashtags = await _hashtagService.GetAllHashtagsAsync();
            return Ok(new BaseResponseModel<List<HashtagResponse>>(
                data: hashtags,
                message: "Danh sách hashtag"
            ));
        }
    }
}