using App.DTOs.DocumentDTOs;

namespace App.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<DocumentResponse> UploadDocumentAsync(DocumentUploadRequest request);
        Task UpdateDocumentVisibilityAsync(string documentId, bool isVisibleToLearner);
        Task DeleteDocumentAsync(string documentId);
    }
}
