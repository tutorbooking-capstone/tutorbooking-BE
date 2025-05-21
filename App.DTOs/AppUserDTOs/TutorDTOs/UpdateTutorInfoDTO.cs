using FluentValidation;

namespace App.DTOs.AppUserDTOs.TutorDTOs
{
    public record UpdateTutorInfoDTO(
        string? NickName,
        string? Brief,
        string? Description,
        string? TeachingMethod
    );

    #region Validator
    public class UpdateTutorInfoDTOValidator : AbstractValidator<UpdateTutorInfoDTO>
    {
        public UpdateTutorInfoDTOValidator()
        {
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
        }
    }
    #endregion

}