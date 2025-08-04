using App.Core.Base;
using App.DTOs.LegalDocumentDTOs;
using App.DTOs.LegalDocumentDTOs.VersionDTOs;
using App.DTOs.LessonDTOs;
using App.Repositories.Models;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LegalDocumentController : ControllerBase
    {
        private readonly ILegalDocumentService _legalDocumentService;
        public LegalDocumentController(ILegalDocumentService legalDocumentService)
        {
            _legalDocumentService = legalDocumentService;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAllAsync(int page = 1, int size = 10)
        {
            var response = await _legalDocumentService.GetAllAsync(page, size);
            return Ok(new BaseResponseModel<IEnumerable<LegalDocumentResponse>>(response, "SUCCESS"));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetByIdAsync(string id)
        {
            var response = await _legalDocumentService.GetByIdAsync(id);
            return Ok(new BaseResponseModel<object>(response, "SUCCESS"));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateAsync([FromBody] LegalDocumentCreateRequest request)
        {
            var response = await _legalDocumentService.CreateAsync(request);
            return Ok(new BaseResponseModel<object>(response, "SUCCESS"));
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateAsync([FromBody] LegalDocumentUpdateRequest request)
        {
            var response = await _legalDocumentService.UpdateAsync(request);
            return Ok(new BaseResponseModel<object>(response, "SUCCESS"));
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            await _legalDocumentService.DeleteAsync(id);
            return Ok(new BaseResponseModel<object>(null, "SUCCESS"));
        }

        [HttpGet("version/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVersionByIdAsync(string id)
        {
            var response = await _legalDocumentService.GetVersionByIdAsync(id);
            return Ok(new BaseResponseModel<object>(response, "SUCCESS"));
        }

        [HttpPost("version")]
        [Authorize]
        public async Task<IActionResult> CreateVersionAsync(LegalDocumentVersionCreateRequest request)
        {
            var response = await _legalDocumentService.CreateVersionAsync(request);
            return Ok(new BaseResponseModel<object>(response, "SUCCESS"));
        }

        [HttpPut("version")]
        [Authorize]
        public async Task<IActionResult> UpdateVersionAsync([FromBody] LegalDocumentVersionUpdateRequest request)
        {
            var response = await _legalDocumentService.UpdateVersionAsync(request);
            return Ok(new BaseResponseModel<object>(response, "SUCCESS"));
        }

        [HttpDelete("version/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteVersionAsync(string id)
        {
            await _legalDocumentService.DeleteVersionAsync(id);
            return Ok(new BaseResponseModel<object>(null, "SUCCESS"));
        }
    }
}
