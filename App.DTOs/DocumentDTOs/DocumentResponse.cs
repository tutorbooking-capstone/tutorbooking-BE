using App.Repositories.Models;
using System;
using System.Collections.Generic;

namespace App.DTOs.DocumentDTOs
{
    public class FileUploadResponse
    {
        public string Id { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string CloudinaryUrl { get; set; } = string.Empty;
    }

    public class DocumentResponse
    {
        public string Id { get; set; } = string.Empty;
        public string ApplicationId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsVisibleToLearner { get; set; }
        public List<FileUploadResponse> Files { get; set; } = new List<FileUploadResponse>();
    }

    #region Mapping
    public static class DocumentResponseExtensions
    {
        public static DocumentResponse ToDocumentResponse(this Document document)
        {
            var response = new DocumentResponse
            {
                Id = document.Id,
                ApplicationId = document.ApplicationId,
                Description = document.Description,
                IsVisibleToLearner = document.IsVisibleToLearner
            };

            if (document.DocumentFileUploads != null)
            {
                foreach (var dfu in document.DocumentFileUploads)
                {
                    response.Files.Add(new FileUploadResponse
                    {
                        Id = dfu.FileUploadId,
                        ContentType = dfu.FileUpload!.ContentType,
                        FileSize = dfu.FileUpload.FileSize,
                        CloudinaryUrl = dfu.FileUpload.CloudinaryUrl
                    });
                }
            }

            return response;
        }
    }
    #endregion
}
