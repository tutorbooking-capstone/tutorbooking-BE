using App.Repositories.Models.Legal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                AcceptanceCount = entity.LegalDocumentAcceptances?.Count ?? 0
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
                AcceptanceCount = entity.LegalDocumentAcceptances?.Count ?? 0
            };
        }
    }
}
