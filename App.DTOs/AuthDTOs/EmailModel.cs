using FluentValidation;

namespace App.DTOs.AuthDTOs
{
    public class EmailModel
    {
        public string Email { get; set; } = string.Empty;
    }

    #region Validator
    public class EmailModelValidator : AbstractValidator<EmailModel>
    {
        public EmailModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email bắt buộc")
                .EmailAddress().WithMessage("Email không hợp lệ");
        }
    }
    #endregion
}
