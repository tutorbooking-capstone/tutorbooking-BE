using App.DTOs.LegalDocumentDTOs;

namespace App.Services.Interfaces
{
    public interface ILegalDocumentAcceptanceService
    {
        Task AcceptAllDocumentsForCurrentUserAsync();
        Task<ICollection<LegalDocumentResponse>> GetLegalDocumentsWithActiveVersionAsync();
        Task<ICollection<LegalDocumentResponse>> GetNotAcceptedLegalDocumentsOfCurrentUserAsync();
    }
}