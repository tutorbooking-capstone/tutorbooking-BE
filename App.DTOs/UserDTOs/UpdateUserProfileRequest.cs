using App.Repositories.Models.User;
using FluentValidation;

namespace App.DTOs.UserDTOs
{
    #region Request Records
    public record UpdateFullNameRequest(string FullName);

    public record UpdateDateOfBirthRequest(DateTime? DateOfBirth);

    public record UpdateGenderRequest(Gender Gender);
    #endregion

    #region Validators
    public class UpdateFullNameRequestValidator : AbstractValidator<UpdateFullNameRequest>
    {
        public UpdateFullNameRequestValidator()
        {
            // Không validate FullName 
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
                .WithMessage("Giới tính không hợp lệ.");
        }
    }
    #endregion
}