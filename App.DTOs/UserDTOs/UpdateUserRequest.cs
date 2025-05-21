using App.Repositories.Models.User;
using FluentValidation;

namespace App.DTOs.UserDTOs
{
    public class UpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public static class UpdateUserRequestExtensions
    {
        public static AppUser ApplyTo(this UpdateUserRequest model, AppUser user)
        {
            if (model.FullName != null)
                user.FullName = model.FullName;

            if (model.PhoneNumber != null)
                user.PhoneNumber = model.PhoneNumber;

            user.LastUpdatedTime = DateTimeOffset.Now;

            return user;
        }
    }

    #region Validator
    public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
    {
        public UpdateUserRequestValidator()
        {
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[0-9]{10,15}$").When(x => !string.IsNullOrEmpty(x.PhoneNumber))
                .WithMessage("Số điện thoại không hợp lệ");
        }
    }
    #endregion
}
