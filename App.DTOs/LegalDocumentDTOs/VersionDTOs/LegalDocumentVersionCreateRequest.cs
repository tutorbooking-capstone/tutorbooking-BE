using App.Repositories.Models.Legal;
using FluentValidation;

namespace App.DTOs.LegalDocumentDTOs.VersionDTOs
{
    public class LegalDocumentVersionCreateRequest
    {
        public string LegalDocumentId { get; set; }
        public string Version { get; set; }
        public LegalDocumentStatus Status { get; set; } = LegalDocumentStatus.Draft;
        public string Content { get; set; }
        public string ContentType { get; set; }
    }

    public static class LegalDocumentVersionCreateRequestExtensions
    {
        public static LegalDocumentVersion ToEntity(this LegalDocumentVersionCreateRequest request)
        {
            return new LegalDocumentVersion
            {
                LegalDocumentId = request.LegalDocumentId,
                Version = request.Version,
                Status = request.Status,
                Content = request.Content,
                ContentType = request.ContentType,
            };
        }
    }

    public class LegalDocumentVersionCreateRequestValidator : AbstractValidator<LegalDocumentVersionCreateRequest>
    {
        public LegalDocumentVersionCreateRequestValidator()
        {
            RuleFor(x => x.LegalDocumentId)
                .NotEmpty().WithMessage("ID_REQUIRED");
            RuleFor(x => x.Version)
                .NotEmpty().WithMessage("VERSION_REQUIRED")
                .MaximumLength(10).WithMessage("VERSION_MAX_10_CHARACTERS");
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("CONTENT_REQUIRED")
                .MaximumLength(10000).WithMessage("CONTENT_MAX_10000_CHARACTERS");
            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("CONTENT_TYPE_REQUIRED")
                .MaximumLength(30).WithMessage("CONTENT_TYPE_MAX_30_CHARACTERS");
        }
    }
}