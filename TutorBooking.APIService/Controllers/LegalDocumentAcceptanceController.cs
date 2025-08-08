using App.Core.Base;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LegalDocumentAcceptanceController : ControllerBase
    {
        private readonly ILegalDocumentAcceptanceService _legalDocumentAcceptanceService;
        public LegalDocumentAcceptanceController(ILegalDocumentAcceptanceService legalDocumentAcceptanceService)
        {
            _legalDocumentAcceptanceService = legalDocumentAcceptanceService;
        }

        [HttpPost("accept")]
        [Authorize]
        public async Task<IActionResult> AcceptDocuments([FromBody] string[] ids)
        {
            await _legalDocumentAcceptanceService.AcceptDocumentsForCurrentUserAsync(ids);
            return Ok(new BaseResponseModel<object>(message: "SUCCESS"));
        }


        [HttpGet("not-accepted-documents")]
        [Authorize]
        public async Task<IActionResult> GetNotAcceptedDocuments(string? category)
        {
            var response = await _legalDocumentAcceptanceService.GetNotAcceptedLegalDocumentsOfCurrentUserAsync(category);
            return Ok(new BaseResponseModel<object>(data: response, message: "SUCCESS"));
        }

        [HttpGet("active-legal-documents")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveLegalDocuments(string? category)
        {
            var response = await _legalDocumentAcceptanceService.GetLegalDocumentsWithActiveVersionAsync(category);
            return Ok(new BaseResponseModel<object>(data: response, message: "SUCCESS"));
        }
    }
}
