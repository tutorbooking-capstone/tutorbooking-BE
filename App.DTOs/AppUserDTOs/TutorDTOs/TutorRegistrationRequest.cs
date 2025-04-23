using FluentValidation;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public class TutorRegistrationRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }  
    }

    #region Validator
    public class TutorRegistrationRequestValidator : AbstractValidator<TutorRegistrationRequest>
    {
        public TutorRegistrationRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Full name is required.")
                .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

            RuleFor(x => x.PhoneNumber)
                .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.")
                .Matches(@"^\+?[0-9\s\-()]*$").WithMessage("Invalid phone number format.")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        }
    }
    #endregion
}