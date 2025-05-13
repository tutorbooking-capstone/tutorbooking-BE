using App.Repositories.Models.Papers;
using FluentValidation;

namespace App.DTOs.ApplicationDTOs.TutorApplicationDTOs
{
    public class TutorApplicationRequest
    {
        //public string Description { get; set; } = string.Empty; 
    }

    #region Validator
    public class TutorApplicationRequestValidator : AbstractValidator<TutorApplicationRequest>
    {
        public TutorApplicationRequestValidator()
        {
            // RuleFor(x => x.Description)
            //     .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters");
        }
    }
    #endregion

    #region Mapping
    public static class TutorApplicationRequestExtensions
    {
        public static TutorApplication ToEntity(this TutorApplicationRequest request, string tutorId)
        {
            return new TutorApplication
            {
                TutorId = tutorId,
                Status = ApplicationStatus.PendingVerification, // Initial status
                SubmittedAt = DateTime.UtcNow
            };
        }
    }
    #endregion
}
