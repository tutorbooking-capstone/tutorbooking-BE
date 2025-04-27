using App.Core.Base;
using App.Repositories.Models;
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
        public IFormFile File { get; set; } = null!;    
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

            RuleFor(x => x.File)
                .NotNull().WithMessage("File is required.")
                .Must(file => file.Length > 0).WithMessage("File cannot be empty.")
                .Must(file => file.Length <= 50 * 1024 * 1024) // 50MB limit
                    .WithMessage("File size cannot exceed 50MB."); 
                // Add content type validation 
                // .Must(file => file.ContentType == "application/pdf" || file.ContentType == "image/jpeg")
                //     .WithMessage("Only PDF and JPG files are allowed.");
        }
    }
    #endregion

    #region Mapping
    public static class DocumentUploadRequestExtensions
    {
        public static Document ToEntity(this DocumentUploadRequest request, string cloudinaryUrl, string userCreateId)
        {
            var newDocument =  new Document
            {
                ApplicationId = request.ApplicationId,
                StaffId = request.StaffId,
                Description = request.Description,
                IsVisibleToLearner = request.IsVisibleToLearner,
                ContentType = request.File.ContentType,
                FileSize = request.File.Length,
                CloudinaryUrl = cloudinaryUrl
            };

            newDocument.TrackCreate(userCreateId);
            return newDocument;
        }
    }
    #endregion

    // No ToEntity mapping needed here as file upload requires service-level logic (Firebase interaction)
}
