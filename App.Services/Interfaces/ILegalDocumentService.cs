using App.DTOs.LegalDocumentDTOs;
using App.DTOs.LegalDocumentDTOs.VersionDTOs;
using System.Runtime.InteropServices;

namespace App.Services.Interfaces
{
    public interface ILegalDocumentService
    {
        Task<LegalDocumentResponse> CreateAsync(LegalDocumentCreateRequest request);
        Task<LegalDocumentVersionResponse> CreateVersionAsync(LegalDocumentVersionCreateRequest request);
        Task DeleteAsync(string id);
        Task DeleteVersionAsync(string id);
        Task<List<LegalDocumentResponse>> GetAllAsync([Optional] string? category, int page = 1, int size = 10);
        Task<LegalDocumentResponse?> GetByIdAsync(string id);
        Task<LegalDocumentVersionResponse?> GetVersionByIdAsync(string id);
        Task<LegalDocumentResponse> UpdateAsync(LegalDocumentUpdateRequest request);
        Task<LegalDocumentVersionResponse?> UpdateVersionAsync(LegalDocumentVersionUpdateRequest request);
    }
}