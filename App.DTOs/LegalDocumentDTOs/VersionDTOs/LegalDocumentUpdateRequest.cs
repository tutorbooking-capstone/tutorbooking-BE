using App.Repositories.Models.Legal;

namespace App.DTOs.LegalDocumentDTOs.VersionDTOs
{
    public class LegalDocumentVersionUpdateRequest
    {
        public string Id { get; set; }
        public string Version { get; set; }
        public LegalDocumentStatus Status { get; set; } = LegalDocumentStatus.Draft;
        public string Content { get; set; }
        public string ContentType { get; set; }
    }

    public static class LegalDocumentVersionUpdateRequestExtensions
    {
        public static void UpdateFromRequest(this LegalDocumentVersion entity, LegalDocumentVersionUpdateRequest request)
        {
            entity.Version = request.Version;
            entity.Status = request.Status;
            entity.Content = request.Content;
            entity.ContentType = request.ContentType;
        }
    }
}
