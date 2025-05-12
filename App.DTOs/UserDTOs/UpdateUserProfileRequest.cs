using App.Repositories.Models.User;
using FluentValidation;

namespace App.DTOs.UserDTOs
{
    #region Request Records
    public record UpdateFullNameRequest(string FullName);

    public record UpdateDateOfBirthRequest(DateTime? DateOfBirth);

    public record UpdateGenderRequest(Gender Gender);

    public record UpdateBasicInformationRequest(
        string? FullName = null,
        DateTime? DateOfBirth = null,
        Gender? Gender = null,
        string? Timezone = null
    );
    #endregion

    #region Validators
    public class UpdateFullNameRequestValidator : AbstractValidator<UpdateFullNameRequest>
    {
        public UpdateFullNameRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Tên không được để trống.")
                .MaximumLength(100).WithMessage("Tên không vượt quá 100 ký tự.");
        }
    }

    public class UpdateDateOfBirthRequestValidator : AbstractValidator<UpdateDateOfBirthRequest>
    {
        public UpdateDateOfBirthRequestValidator()
        {
            RuleFor(x => x.DateOfBirth)
                .Must(dob => dob == null || dob < DateTime.Now)
                .WithMessage("Ngày sinh không được ở tương lai.");
        }
    }

    public class UpdateGenderRequestValidator : AbstractValidator<UpdateGenderRequest>
    {
        public UpdateGenderRequestValidator()
        {
            RuleFor(x => x.Gender)
                .IsInEnum()
                .WithMessage("Giới tính không hợp lệ (0 - Khác, 1 - Nam, 2 - Nữ).");
        }
    }

    public class UpdateBasicInformationRequestValidator : AbstractValidator<UpdateBasicInformationRequest>
    {
        public UpdateBasicInformationRequestValidator()
        {
            When(x => x.FullName != null, () => {
                RuleFor(x => x.FullName)
                    .NotEmpty().WithMessage("Tên không được để trống.")
                    .MaximumLength(100).WithMessage("Tên không vượt quá 100 ký tự.");
            });

            RuleFor(x => x.DateOfBirth)
                .Must(dob => dob == null || dob < DateTime.Now)
                .When(x => x.DateOfBirth.HasValue)
                .WithMessage("Ngày sinh không được ở tương lai.");

            RuleFor(x => x.Gender)
                .IsInEnum()
                .When(x => x.Gender.HasValue)
                .WithMessage("Giới tính không hợp lệ (0 - Khác, 1 - Nam, 2 - Nữ).");
        }
    }
    #endregion
}