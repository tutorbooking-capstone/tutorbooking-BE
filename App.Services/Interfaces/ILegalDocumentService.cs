using App.DTOs.LegalDocumentDTOs;
using App.DTOs.LegalDocumentDTOs.VersionDTOs;

namespace App.Services.Interfaces
{
    public interface ILegalDocumentService
    {
        Task<LegalDocumentResponse> CreateAsync(LegalDocumentCreateRequest request);
        Task<LegalDocumentVersionResponse> CreateVersionAsync(LegalDocumentVersionCreateRequest request);
        Task DeleteAsync(string id);
        Task DeleteVersionAsync(string id);
        Task<List<LegalDocumentResponse>> GetAllAsync(int page, int size);
        Task<LegalDocumentResponse?> GetByIdAsync(string id);
        Task<LegalDocumentVersionResponse?> GetVersionByIdAsync(string id);
        Task<LegalDocumentResponse> UpdateAsync(LegalDocumentUpdateRequest request);
        Task<LegalDocumentVersionResponse?> UpdateVersionAsync(LegalDocumentVersionUpdateRequest request);
    }
}