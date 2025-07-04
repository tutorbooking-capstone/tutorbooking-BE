using App.Core.Base;
using App.Core.Constants;
using App.Core.Provider;
using App.DTOs.DocumentDTOs;
using App.Repositories.Models;
using App.Repositories.Models.Papers;
using App.Repositories.UoW;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace App.Services.Services
{
    internal class DocumentService : IDocumentService
    {
        #region DI Constructor
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryProvider _cloudinaryProvider;

        public DocumentService(
            IUnitOfWork unitOfWork,
            ICloudinaryProvider cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryProvider = cloudinaryService;
        }
        #endregion

        public async Task<DocumentResponse> UploadDocumentAsync(DocumentUploadRequest request)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () => 
            {
                var document = request.ToDocumentEntity();
                _unitOfWork.GetRepository<Document>().Insert(document);
                await _unitOfWork.SaveAsync(); // Save first to get Document ID

                var fileUploads = new List<FileUpload>();
                var documentFileUploads = new List<DocumentFileUpload>();

                foreach (var file in request.Files)
                {
                    var cloudinaryUrl = await _cloudinaryProvider.UploadDocumentAsync(file);
                    var fileUpload = file.ToFileUploadEntity(cloudinaryUrl);
                    
                    fileUploads.Add(fileUpload);
                    documentFileUploads.Add(fileUpload.ToDocumentFileUploadEntity(document.Id));
                }

                _unitOfWork.GetRepository<FileUpload>().InsertRange(fileUploads);
                _unitOfWork.GetRepository<DocumentFileUpload>().InsertRange(documentFileUploads);
                await _unitOfWork.SaveAsync();

                return document.ToDocumentResponse();
            });
        }

        // public async Task<List<DocumentResponse>> UploadListDocumentsAsync(List<DocumentUploadRequest> requests)
        // {
        //     var documents = new List<DocumentResponse>();

        //     foreach (var request in requests)
        //     {
        //         string cloudinaryUrl = await _cloudinaryProvider.UploadDocumentAsync(request.File);
        //         var document = request.ToEntity(cloudinaryUrl);
        //         _unitOfWork.GetRepository<Document>().Insert(document);
        //         documents.Add(document.ToDocumentResponse());
        //     }

        //     await _unitOfWork.SaveAsync();
        //     return documents;
        // }

        public async Task UpdateDocumentVisibilityAsync(string documentId, bool isVisibleToLearner)
        {
            var document = await GetDocumentByIdAsync(documentId);
            var modifiedProperties = document.UpdateVisibility(isVisibleToLearner);

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

        public async Task<ICollection<DocumentResponse>> GetDocumentsByTutorIdAsync(string tutorId)
        {
            var documets = await _unitOfWork.GetRepository<Document>().ExistEntities()
                .Include(e => e.DocumentFileUploads).ThenInclude(d => d.FileUpload)
                .Where(e => e.Application.TutorId.Equals(tutorId)).ToListAsync();
            return documets.ToDocumentResponses();
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
