using App.Repositories.Models.Legal;

namespace App.DTOs.LegalDocumentDTOs.VersionDTOs
{
    public class LegalDocumentVersionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string LegalDocumentId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public LegalDocumentStatus Status { get; set; } = LegalDocumentStatus.Draft;
        public string Content { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public int AcceptanceCount { get; set; } = 0;
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; } 
    }

    public static class LegalDocumentVersionResponseExtensions
    {
        public static LegalDocumentVersionResponse ToResponse(this LegalDocumentVersion entity)
        {
            return new LegalDocumentVersionResponse
            {
                Id = entity.Id,
                LegalDocumentId = entity.LegalDocumentId,
                Version = entity.Version,
                Status = entity.Status,
                Content = entity.Content,
                ContentType = entity.ContentType,
                AcceptanceCount = entity.LegalDocumentAcceptances?.Count ?? 0,
                CreatedTime = entity.CreatedTime,
                LastUpdatedTime = entity.LastUpdatedTime
            };
        }

        public static LegalDocumentVersionResponse ToResponseWithoutContent(this LegalDocumentVersion entity)
        {
            return new LegalDocumentVersionResponse
            {
                Id = entity.Id,
                LegalDocumentId = entity.LegalDocumentId,
                Version = entity.Version,
                Status = entity.Status,
                Content = "HIDDEN",
                ContentType = "HIDDEN",
                AcceptanceCount = entity.LegalDocumentAcceptances?.Count ?? 0,
                CreatedTime = entity.CreatedTime,
                LastUpdatedTime = entity.LastUpdatedTime
            };
        }
    }
}
