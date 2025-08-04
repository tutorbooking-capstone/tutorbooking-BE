using App.Repositories.Models.Legal;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.LegalDocumentDTOs
{
    public class LegalDocumentUpdateRequest
    {
        public string Id { get; set; } 
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public static class LegalDocumentUpdateRequestExtensions
    {
        public static void UpdateFromRequest(this LegalDocument entity, LegalDocumentUpdateRequest request)
        {
            entity.Name = request.Name;
            entity.Description = request.Description;
        }
    }

    public class LegalDocumentUpdateRequestValidator : AbstractValidator<LegalDocumentUpdateRequest>
    {
        public LegalDocumentUpdateRequestValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("ID_REQUIRED");
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("NAME_REQUIRED")
                .MaximumLength(100).WithMessage("NAME_MAX_100_CHARACTERS");
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("DESCRIPTION_REQUIRED")
                .MaximumLength(500).WithMessage("DESCRIPTION_MAX_500_CHARACTERS");
        }
    }
}
