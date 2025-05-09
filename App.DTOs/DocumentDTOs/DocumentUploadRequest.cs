using App.Repositories.Models;
using App.Repositories.Models.Papers;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace App.DTOs.DocumentDTOs
{
    public class DocumentUploadRequest
    {
        public string ApplicationId { get; set; } = string.Empty;
        public string? StaffId { get; set; } = null;
        public bool IsVisibleToLearner { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<IFormFile> Files { get; set; } = new List<IFormFile>();
    }

    #region Validator
    public class DocumentUploadRequestValidator : AbstractValidator<DocumentUploadRequest>
    {
        public DocumentUploadRequestValidator()
        {
            RuleFor(x => x.ApplicationId)
                .NotEmpty().WithMessage("Application ID is required.");

            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(255).WithMessage("Description cannot exceed 255 characters.");

            RuleFor(x => x.Files)
                .NotEmpty().WithMessage("File is required.")
                .Must(files => files.All(f => f.Length > 0))
                .Must(files => files.All(f => f.Length <= 50 * 1024 * 1024)) 
                    .WithMessage("File size cannot exceed 50MB.")
                .Must(files => files.All(f => 
                    f.ContentType.StartsWith("image/") || 
                    f.ContentType.StartsWith("video/") ||
                    f.ContentType == "application/pdf" ||
                    f.ContentType == "application/msword" ||
                    f.ContentType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document"))
                .WithMessage("Only images, video, PDFs and Word documents are allowed");
        }
    }
    #endregion

    #region Mapping
    public static class DocumentUploadRequestExtensions
    {
        public static Document ToDocumentEntity(this DocumentUploadRequest request)
        {
            return new Document
            {
                ApplicationId = request.ApplicationId,
                StaffId = request.StaffId,
                Description = request.Description,
                IsVisibleToLearner = request.IsVisibleToLearner
            };
        }

        public static FileUpload ToFileUploadEntity(this IFormFile file, string cloudinaryUrl)
        {
            return new FileUpload
            {
                ContentType = file.ContentType,
                FileSize = file.Length,
                CloudinaryUrl = cloudinaryUrl,
                OriginalFileName = file.FileName
            };
        }

        public static DocumentFileUpload ToDocumentFileUploadEntity(this FileUpload fileUpload, string documentId)
        {
            return new DocumentFileUpload
            {
                DocumentId = documentId,
                FileUploadId = fileUpload.Id,
                FileUpload = fileUpload
            };
        }
    }
    #endregion

    // No ToEntity mapping needed here as file upload requires service-level logic (Firebase interaction)
}
