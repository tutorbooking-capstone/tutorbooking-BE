using App.DTOs.DocumentDTOs;

namespace App.Services.Interfaces
{
    public interface IDocumentService
    {
        Task<DocumentResponse> UploadDocumentAsync(DocumentUploadRequest request);
        //Task<List<DocumentResponse>> UploadListDocumentsAsync(List<DocumentUploadRequest> requests);
        Task UpdateDocumentVisibilityAsync(string documentId, bool isVisibleToLearner);
        Task DeleteDocumentAsync(string documentId);
        Task VerifyDocumentAsync(DocumentVerifyRequest request);
        Task VerifyDocumentListAsync(DocumentVerifyRequestList requestList);
    }
}
