using App.Repositories.Models.User;
using FluentValidation;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public record TutorRegistrationRequest(
        // Step 1: Basic Info Validation
        string FullName,
        DateTime? DateOfBirth,
        Gender? Gender,
        string Timezone = "UTC+7",

        // Step 2: Tutor Info Validation
        string? NickName = null,
        string? Brief = null,
        string? Description = null,
        string? TeachingMethod = null,

        // Step 3: Hashtag Validation
        List<string>? HashtagIds = null,

        // Step 4: Languages Validation
        List<TutorLanguageDTO> Languages = null!
    );

    #region Validator
    public class TutorRegistrationRequestValidator : AbstractValidator<TutorRegistrationRequest>
    {
        public TutorRegistrationRequestValidator()
        {
            // Step 1: Basic Info Validation
            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Họ tên không được để trống")
                .MaximumLength(100).WithMessage("Họ tên không quá 100 ký tự");

            RuleFor(x => x.DateOfBirth)
                .Must(dob => dob == null || dob < DateTime.Now)
                .WithMessage("Ngày sinh không hợp lệ");

            RuleFor(x => x.Gender)
                .IsInEnum().WithMessage("Giới tính không hợp lệ");

            RuleFor(x => x.Timezone)
                .NotEmpty().WithMessage("Múi giờ không được để trống");

            // Step 2: Tutor Info Validation
            RuleFor(x => x.NickName)
                .MaximumLength(50).WithMessage("Biệt danh không quá 50 ký tự")
                .When(x => x.NickName != null);

            RuleFor(x => x.Brief)
                .MaximumLength(300).WithMessage("Giới thiệu ngắn không quá 300 ký tự")
                .When(x => x.Brief != null);

            RuleFor(x => x.Description)
                .MaximumLength(3000).WithMessage("Mô tả không quá 3000 ký tự")
                .When(x => x.Description != null);

            RuleFor(x => x.TeachingMethod)
                .MaximumLength(300).WithMessage("Phương pháp giảng dạy không quá 300 ký tự")
                .When(x => x.TeachingMethod != null);

            // Step 3: Hashtag Validation
            RuleFor(x => x.HashtagIds)
                .Must(ids => ids == null || ids.Count <= 10)
                .WithMessage("Tối đa 10 hashtag được chọn")
                .When(x => x.HashtagIds != null);

            // Step 4: Languages Validation
            RuleFor(x => x.Languages)
                .NotEmpty().WithMessage("Phải chọn ít nhất một ngôn ngữ để dạy")
                .When(x => x.Languages != null);
                
            RuleForEach(x => x.Languages)
                .SetValidator(new TutorLanguageDTOValidator())
                .When(x => x.Languages != null);
        }
    }
    #endregion

    #region Extensions
    public static class TutorRegistrationRequestExtensions
    {
        public static Tutor ToTutorProfile(this TutorRegistrationRequest request, AppUser user)
        {
            return new Tutor
            {
                UserId = user.Id,
                NickName = request.NickName ?? string.Empty,
                Brief = request.Brief ?? string.Empty,
                Description = request.Description ?? string.Empty,
                TeachingMethod = request.TeachingMethod ?? string.Empty,
                //VerificationStatus = VerificationStatus.Basic,
                BecameTutorAt = DateTime.UtcNow,
                LastStatusUpdateAt = DateTime.UtcNow,
                User = user
            };
        }
    }
    #endregion
}