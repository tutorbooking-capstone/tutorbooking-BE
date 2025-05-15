using App.Core.Base;
using App.DTOs.AppUserDTOs.LearnerDTOs;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/learner")]
    [ApiController]
    [Authorize]
    public class LearnerController : ControllerBase
    {
        #region DI Constructor
        private readonly ILearnerService _learnerService;

        public LearnerController(ILearnerService learnerService)
        {
            _learnerService = learnerService;
        }
        #endregion

        //[HttpPatch("language")]
        //public async Task<IActionResult> UpdateLearningLanguage([FromBody] UpdateLearnerLanguageRequest request)
        //{
        //    await _learnerService.UpdateLearningLanguageAsync(request);
        //    return Ok(new BaseResponseModel<string>(
        //        message: "Ngôn ngữ học tập đã được cập nhật thành công."
        //    ));
        //}

        //[HttpGet("language")]
        //public async Task<IActionResult> GetLearningLanguage()
        //{
        //    var (languageCode, proficiencyLevel) = await _learnerService.GetLearningLanguageAsync();
        //    return Ok(new BaseResponseModel<object>(
        //        data: new { LanguageCode = languageCode, ProficiencyLevel = proficiencyLevel },
        //        message: "Thông tin ngôn ngữ học tập."
        //    ));
        //}
    }
}
