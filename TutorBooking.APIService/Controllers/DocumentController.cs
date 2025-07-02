using App.Core.Base;
using App.DTOs.DocumentDTOs;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TutorBooking.APIService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DocumentController : ControllerBase
    {
        #region DI Constructor
        private readonly IDocumentService _documentService;

        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }
        #endregion

        [HttpPost("upload")]
        [Authorize]
        public async Task<IActionResult> UploadDocuments([FromForm] DocumentUploadRequest request)
        {
            var document = await _documentService.UploadDocumentAsync(request);
            return Ok(new BaseResponseModel<DocumentResponse>(
                data: document,
                message: "Tải lên tài liệu thành công!"
            ));
        }

        [HttpPatch("{documentId}/visibility")]
        [Authorize]
        public async Task<IActionResult> UpdateDocumentVisibility(string documentId, [FromBody] bool isVisibleToLearner)
        {
            await _documentService.UpdateDocumentVisibilityAsync(documentId, isVisibleToLearner);
            return Ok(new BaseResponseModel<string>(
                message: "Cập nhật hiển thị tài liệu thành công!"
            ));
        }

        [HttpDelete("{documentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteDocument(string documentId)
        {
            await _documentService.DeleteDocumentAsync(documentId);
            return Ok(new BaseResponseModel<string>(
                message: "Xóa tài liệu thành công!"
            ));
        }

        [HttpGet("tutor/{tutorId}")]
        [Authorize]
        public async Task<IActionResult> GetDocumentsByTutorId(string tutorId)
        {
            var documents = await _documentService.GetDocumentsByTutorIdAsync(tutorId);
            return Ok(new BaseResponseModel<object>(
                data: documents,
                message: "SUCCEXSS"
            ));
        }
    }
}