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
        [HttpPost("accept-all")]
        [Authorize]
        public async Task<IActionResult> AcceptAllDocuments()
        {
            await _legalDocumentAcceptanceService.AcceptAllDocumentsForCurrentUserAsync();
            return Ok(new BaseResponseModel<object>(null, "SUCCESS"));
        }


        [HttpGet("not-accepted-documents")]
        [Authorize]
        public async Task<IActionResult> GetNotAcceptedDocuments()
        {
            var response = await _legalDocumentAcceptanceService.GetNotAcceptedLegalDocumentsOfCurrentUserAsync();
            return Ok(new BaseResponseModel<object>(response, "SUCCESS"));
        }

        [HttpGet("active-legal-documents")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveLegalDocuments()
        {
            var response = await _legalDocumentAcceptanceService.GetLegalDocumentsWithActiveVersionAsync();
            return Ok(new BaseResponseModel<object>(response, "SUCCESS"));
        }
    }
}
