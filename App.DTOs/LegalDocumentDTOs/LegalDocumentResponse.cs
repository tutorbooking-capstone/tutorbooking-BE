using App.DTOs.LegalDocumentDTOs.VersionDTOs;
using App.Repositories.Models.Legal;

namespace App.DTOs.LegalDocumentDTOs
{
    public class LegalDocumentResponse 
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset CreatedTime { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
        public ICollection<LegalDocumentVersionResponse>? Versions { get; set; } = new List<LegalDocumentVersionResponse>();
    }

    public static class LegalDocumentResponseExtensions
    {
        public static LegalDocumentResponse ToResponseWithLatestActiveVersionOnly(this LegalDocument entity)
        {
            return new()
            {
                Id = entity.Id,
                Name = entity.Name,
                Category = entity.Category,
                Description = entity.Description,
                Versions = entity.Versions?
                    .Where(e => e.Status == LegalDocumentStatus.Active)
                    .OrderByDescending(e => e.LastUpdatedTime)
                    .Take(1)
                    .Select(v => v.ToResponseWithoutContent()).
                    AsEnumerable()
                    .ToList(),
                CreatedTime = entity.CreatedTime,
                LastUpdatedTime = entity.LastUpdatedTime,
            };
        }

        public static LegalDocumentResponse ToResponse(this LegalDocument entity)
        {
            return new()
            {
                Id = entity.Id,
                Name = entity.Name,
                Category = entity.Category,
                Description = entity.Description,
                Versions = entity.Versions?.Select(v => v.ToResponseWithoutContent()).ToList(),
                CreatedTime = entity.CreatedTime,
                LastUpdatedTime = entity.LastUpdatedTime,
            };
        }
    }
}
