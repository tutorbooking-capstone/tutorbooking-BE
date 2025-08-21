using App.Repositories.Models.Legal;
using FluentValidation;

namespace App.DTOs.LegalDocumentDTOs
{
    public class LegalDocumentCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public static class LegalDocumentCreateRequestExtenstions
    {
        public static LegalDocument ToEntity(this LegalDocumentCreateRequest request)
        {
            return new LegalDocument
            {
                Name = request.Name,
                Category = request.Category,
                Description = request.Description,
            };
        }
    }

    public class LegalDocumentCreateRequestValidator : AbstractValidator<LegalDocumentCreateRequest>
    {
        public LegalDocumentCreateRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("NAME_REQUIRED")
                .MaximumLength(100).WithMessage("NAME_MAX_100_CHARACTERS");
            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("CATEGORY_REQUIRED")
                .MaximumLength(100).WithMessage("CATEGORY_MAX_100_CHARACTERS");
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("DESCRIPTION_REQUIRED")
                .MaximumLength(500).WithMessage("DESCRIPTION_MAX_500_CHARACTERS");
        }
    }
}
