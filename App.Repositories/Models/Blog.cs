using App.Core.Base;
using App.Repositories.Models.User;
using FluentValidation;

namespace App.Repositories.Models
{
    public class Blog : BaseEntity
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int LikeCount { get; set; } = 0;
        public string UserId { get; set; } = string.Empty;
        
        public virtual AppUser? AppUser { get; set; }
    }

    #region Validator
    public class BlogValidator : AbstractValidator<Blog>
    {
        public BlogValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Tiêu đề không được để trống")
                .MaximumLength(200).WithMessage("Tiêu đề không được quá 200 ký tự");

            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Nội dung không được để trống");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId không được để trống");
        }
    }
    #endregion
}
