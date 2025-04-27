using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.DocumentDTOs;
using App.Repositories.Models;
using App.Repositories.UoW;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services
{
    internal class DocumentService : IDocumentService
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserService _userService;
        private readonly ICloudinaryProvider _cloudinaryProvider;

        public DocumentService(
            IUnitOfWork unitOfWork, 
            IUserService userService,
            ICloudinaryProvider cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _userService = userService;
            _cloudinaryProvider = cloudinaryService;
        }
        #endregion

        public async Task<DocumentResponse> UploadDocumentAsync(DocumentUploadRequest request)
        {
            var userUploadId = _userService.GetCurrentUserId();

            string cloudinaryUrl = await _cloudinaryProvider.UploadDocumentAsync(request.File);

            var document = request.ToEntity(cloudinaryUrl, userUploadId);

            _unitOfWork.GetRepository<Document>().Insert(document);
            await _unitOfWork.SaveAsync();

            return document.ToDocumentResponse();

        }

        public async Task UpdateDocumentVisibilityAsync(string documentId, bool isVisibleToLearner)
        {
            var document = await GetDocumentByIdAsync(documentId);
            var modifiedProperties = document.UpdateDocumentInfo(
                document.Description,
                isVisibleToLearner,
                document.ContentType,
                document.FileSize,
                document.CloudinaryUrl);

            if (modifiedProperties.Length > 0)
            {
                _unitOfWork.GetRepository<Document>().UpdateFields(document, modifiedProperties);
                await _unitOfWork.SaveAsync();
            }
        }

        public async Task DeleteDocumentAsync(string documentId)
        {
            var document = await GetDocumentByIdAsync(documentId);
            _unitOfWork.GetRepository<Document>().Delete(document);
            await _unitOfWork.SaveAsync();
        }

        #region Private Helper
        private async Task<Document> GetDocumentByIdAsync(string documentId)
        {
            var document = await _unitOfWork.GetRepository<Document>()
                .ExistEntities()
                .FirstOrDefaultAsync(d => d.Id == documentId);

            if (document == null)
                throw new ErrorException(
                    StatusCodes.Status404NotFound,
                    ErrorCode.NotFound,
                    $"Document with ID {documentId} not found.");

            return document;
        }
        #endregion
    }
}
