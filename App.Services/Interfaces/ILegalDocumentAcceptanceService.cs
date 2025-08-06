using App.DTOs.LegalDocumentDTOs;

namespace App.Services.Interfaces
{
    public interface ILegalDocumentAcceptanceService
    {
        Task AcceptDocumentsForCurrentUserAsync(ICollection<string> documentIds);
        Task<ICollection<LegalDocumentResponse>> GetLegalDocumentsWithActiveVersionAsync(string? category);
        Task<ICollection<LegalDocumentResponse>> GetNotAcceptedLegalDocumentsOfCurrentUserAsync(string? category);
    }
}