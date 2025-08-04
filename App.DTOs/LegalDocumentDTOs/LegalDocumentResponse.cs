using App.Core.Base;
using App.DTOs.LegalDocumentDTOs.VersionDTOs;
using App.Repositories.Models.Legal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.LegalDocumentDTOs
{
    public class LegalDocumentResponse : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
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
                DeletedTime = entity.DeletedTime,
            };
        }

        public static LegalDocumentResponse ToResponse(this LegalDocument entity)
        {
            return new()
            {
                Id = entity.Id,
                Name = entity.Name,
                Description = entity.Description,
                Versions = entity.Versions?.Select(v => v.ToResponseWithoutContent()).ToList(),
                CreatedTime = entity.CreatedTime,
                LastUpdatedTime = entity.LastUpdatedTime,
                DeletedTime = entity.DeletedTime,
            };
        }
    }
}
