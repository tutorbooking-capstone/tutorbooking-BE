using App.Repositories.Models;
using System;

namespace App.DTOs.DocumentDTOs
{
    public class DocumentResponse
    {
        public string Id { get; set; } = string.Empty;
        public string ApplicationId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; }
        public string CloudinaryUrl { get; set; } = string.Empty;
        public bool IsVisibleToLearner { get; set; }
    }

    #region Mapping
    public static class DocumentResponseExtensions
    {
        public static DocumentResponse ToDocumentResponse(this Document entity)
        {
            return new DocumentResponse
            {
                Id = entity.Id,
                ApplicationId = entity.ApplicationId,
                Description = entity.Description,
                ContentType = entity.ContentType,
                FileSize = entity.FileSize,
                UploadedAt = entity.UploadedAt,
                CloudinaryUrl = entity.CloudinaryUrl, 
                IsVisibleToLearner = entity.IsVisibleToLearner
            };
        }
    }
    #endregion
}
