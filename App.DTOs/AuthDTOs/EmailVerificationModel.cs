using FluentValidation;

namespace App.DTOs.AuthDTOs
{
    public class EmailVerificationModel
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    #region Validator
    public class EmailVerificationModelValidator : AbstractValidator<EmailVerificationModel>
    {
        public EmailVerificationModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email bắt buộc")
                .EmailAddress().WithMessage("Email không hợp lệ");

            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token bắt buộc");
        }
    }
    #endregion
}
