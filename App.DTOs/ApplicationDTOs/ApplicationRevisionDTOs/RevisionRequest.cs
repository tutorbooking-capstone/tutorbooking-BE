using App.Repositories.Models;
using FluentValidation;

namespace App.DTOs.ApplicationDTOs.ApplicationRevisionDTOs
{
    public class RevisionRequest
    {
        public string ApplicationId { get; set; } = string.Empty;
        public RevisionAction Action { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    #region Validator
    public class RevisionRequestValidator : AbstractValidator<RevisionRequest>
    {
        public RevisionRequestValidator()
        {
            RuleFor(x => x.ApplicationId)
                .NotEmpty().WithMessage("Application ID is required");

            RuleFor(x => x.Action)
                .IsInEnum().WithMessage("Invalid action value");

            RuleFor(x => x.Notes)
                .MaximumLength(500).WithMessage("Notes cannot exceed 500 characters");
        }
    }
    #endregion

    #region Mapping
    public static class RevisionRequestExtensions
    {
        public static ApplicationRevision ToEntity(this RevisionRequest request, string staffId)
        {
            return new ApplicationRevision
            {
                ApplicationId = request.ApplicationId,
                Action = request.Action,
                Notes = request.Notes,
                StaffId = staffId,
                Timestamp = DateTime.UtcNow
            };
        }
    }
    #endregion
}
