using FluentValidation;

namespace App.DTOs.AuthDTOs
{
    public class ConfirmOTPRequest
    {
        public string Email { get; set; } = string.Empty;
        public string OTP { get; set; } = string.Empty;
    }

    #region Validator
    public class ConfirmOTPRequestValidator : AbstractValidator<ConfirmOTPRequest>
    {
        public ConfirmOTPRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email bắt buộc")
                .EmailAddress().WithMessage("Email không hợp lệ");

            RuleFor(x => x.OTP)
                .NotEmpty().WithMessage("OTP bắt buộc")
                .Length(6).WithMessage("OTP không hợp lệ")
                .Matches(@"^\d{6}$").WithMessage("OTP không hợp lệ");
        }
    }
    #endregion
}
