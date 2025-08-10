using App.Core.Base;
using App.DTOs.LegalDocumentDTOs;
using App.DTOs.LegalDocumentDTOs.VersionDTOs;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> GetAllAsync(string? category, int page = 1, int size = 10)
        {
            var response = await _legalDocumentService.GetAllAsync(category, page, size);
            return Ok(new BaseResponseModel<IEnumerable<LegalDocumentResponse>>(response,null, "SUCCESS"));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetByIdAsync(string id)
        {
            var response = await _legalDocumentService.GetByIdAsync(id);
            return Ok(new BaseResponseModel<object>(response, null, "SUCCESS"));
        }

        [HttpGet("all-categories")]
        [Authorize]
        public async Task<IActionResult> GetAllCategoriesAsync()
        {
            var response = await _legalDocumentService.GetAllCategoriesAsync();
            return Ok(new BaseResponseModel<ICollection<string>>(response, null, "SUCCESS"));
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateAsync([FromBody] LegalDocumentCreateRequest request)
        {
            var response = await _legalDocumentService.CreateAsync(request);
            return Ok(new BaseResponseModel<object>(response, null, "SUCCESS"));
        }

        [HttpPut]
        [Authorize]
        public async Task<IActionResult> UpdateAsync([FromBody] LegalDocumentUpdateRequest request)
        {
            var response = await _legalDocumentService.UpdateAsync(request);
            return Ok(new BaseResponseModel<object>(response, null, "SUCCESS"));
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            await _legalDocumentService.DeleteAsync(id);
            return Ok(new BaseResponseModel<object>(null, null, "SUCCESS"));
        }

        [HttpGet("version/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetVersionByIdAsync(string id)
        {
            var response = await _legalDocumentService.GetVersionByIdAsync(id);
            return Ok(new BaseResponseModel<object>(response, null, "SUCCESS"));
        }

        [HttpPost("version")]
        [Authorize]
        public async Task<IActionResult> CreateVersionAsync(LegalDocumentVersionCreateRequest request)
        {
            var response = await _legalDocumentService.CreateVersionAsync(request);
            return Ok(new BaseResponseModel<object>(response, null, "SUCCESS"));
        }

        [HttpPut("version")]
        [Authorize]
        public async Task<IActionResult> UpdateVersionAsync([FromBody] LegalDocumentVersionUpdateRequest request)
        {
            var response = await _legalDocumentService.UpdateVersionAsync(request);
            return Ok(new BaseResponseModel<object>(response, null, "SUCCESS"));
        }

        [HttpDelete("version/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteVersionAsync(string id)
        {
            await _legalDocumentService.DeleteVersionAsync(id);
            return Ok(new BaseResponseModel<object>(null, null, "SUCCESS"));
        }
    }
}
