using FluentValidation;

namespace App.DTOs.AppUserDTOs.LearnerDTOs
{
    public record UpdateLearnerLanguageRequest(
        string? LanguageCode = null,
        int? ProficiencyLevel = null
    );

    public class UpdateLearnerLanguageRequestValidator : AbstractValidator<UpdateLearnerLanguageRequest>
    {
        public UpdateLearnerLanguageRequestValidator()
        {
            When(x => x.LanguageCode != null, () => {
                RuleFor(x => x.LanguageCode)
                    .NotEmpty().WithMessage("Mã ngôn ngữ không được để trống.")
                    .MaximumLength(10).WithMessage("Mã ngôn ngữ không vượt quá 10 ký tự.");
            });

            When(x => x.ProficiencyLevel.HasValue, () => {
                RuleFor(x => x.ProficiencyLevel)
                    .InclusiveBetween(1, 7)
                    .WithMessage("Mức độ thông thạo phải nằm trong khoảng từ 1 đến 7.");
            });
        }
    }
}
