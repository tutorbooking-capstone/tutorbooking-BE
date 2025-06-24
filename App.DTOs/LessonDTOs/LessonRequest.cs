using App.Repositories.Models;
using FluentValidation;

namespace App.DTOs.LessonDTOs
{
    public class CreateLessonRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Note { get; set; }

        public string TargetAudience { get; set; } = string.Empty;
        public string Prerequisites { get; set; } = string.Empty;

        public string LanguageCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        
        public decimal Price { get; set; }
        public string Currency { get; set; } = "VND";
        //public int DurationInMinutes { get; set; } = 30;
    }

    public class UpdateLessonRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Note { get; set; }

        public string? TargetAudience { get; set; }
        public string? Prerequisites { get; set; }

        public string? LanguageCode { get; set; }
        public string? Category { get; set; }
        
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        //public int? DurationInMinutes { get; set; }
    }

    #region Validator
    public class CreateLessonRequestValidator : AbstractValidator<CreateLessonRequest>
    {
        public CreateLessonRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên bài học không được để trống")
                .MaximumLength(100).WithMessage("Tên bài học không được vượt quá 100 ký tự");
                
            RuleFor(x => x.Description)
                .NotEmpty().WithMessage("Mô tả không được để trống")
                .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự");
                
            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự");
                
            RuleFor(x => x.TargetAudience)
                .NotEmpty().WithMessage("Đối tượng mục tiêu không được để trống")
                .MaximumLength(100).WithMessage("Đối tượng mục tiêu không được vượt quá 100 ký tự");
                
            RuleFor(x => x.Prerequisites)
                .MaximumLength(500).WithMessage("Yêu cầu trước không được vượt quá 500 ký tự");
                
            RuleFor(x => x.LanguageCode)
                .NotEmpty().WithMessage("Mã ngôn ngữ không được để trống")
                .Length(2, 5).WithMessage("Mã ngôn ngữ phải có từ 2 đến 5 ký tự");
                
            RuleFor(x => x.Category)
                .NotEmpty().WithMessage("Danh mục không được để trống")
                .MaximumLength(50).WithMessage("Danh mục không được vượt quá 50 ký tự");
                
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Giá phải lớn hơn hoặc bằng 0");
                
            RuleFor(x => x.Currency)
                .NotEmpty().WithMessage("Tiền tệ không được để trống")
                .Length(3).WithMessage("Tiền tệ phải có 3 ký tự");
                
            // RuleFor(x => x.DurationInMinutes)
            //     .GreaterThan(0).WithMessage("Thời lượng phải lớn hơn 0 phút");
        }
    }

    public class UpdateLessonRequestValidator : AbstractValidator<UpdateLessonRequest>
    {
        public UpdateLessonRequestValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("Tên bài học không được vượt quá 100 ký tự")
                .When(x => x.Name != null);
                
            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Mô tả không được vượt quá 1000 ký tự")
                .When(x => x.Description != null);
                
            RuleFor(x => x.Note)
                .MaximumLength(500).WithMessage("Ghi chú không được vượt quá 500 ký tự")
                .When(x => x.Note != null);
                
            RuleFor(x => x.TargetAudience)
                .MaximumLength(100).WithMessage("Đối tượng mục tiêu không được vượt quá 100 ký tự")
                .When(x => x.TargetAudience != null);
                
            RuleFor(x => x.Prerequisites)
                .MaximumLength(500).WithMessage("Yêu cầu trước không được vượt quá 500 ký tự")
                .When(x => x.Prerequisites != null);
                
            RuleFor(x => x.LanguageCode)
                .Length(2, 5).WithMessage("Mã ngôn ngữ phải có từ 2 đến 5 ký tự")
                .When(x => x.LanguageCode != null);
                
            RuleFor(x => x.Category)
                .MaximumLength(50).WithMessage("Danh mục không được vượt quá 50 ký tự")
                .When(x => x.Category != null);
                
            RuleFor(x => x.Price)
                .GreaterThanOrEqualTo(0).WithMessage("Giá phải lớn hơn hoặc bằng 0")
                .When(x => x.Price.HasValue);
                
            RuleFor(x => x.Currency)
                .Length(3).WithMessage("Tiền tệ phải có 3 ký tự")
                .When(x => x.Currency != null);
                
            // RuleFor(x => x.DurationInMinutes)
            //     .GreaterThan(0).WithMessage("Thời lượng phải lớn hơn 0 phút")
            //     .When(x => x.DurationInMinutes.HasValue);
        }
    }
    #endregion

    #region Mapping
    public static class LessonRequestExtensions
    {
        public static Lesson ToLessonEntity(this CreateLessonRequest request, string tutorId)
        {
            return new Lesson
            {
                TutorId = tutorId,
                Name = request.Name,
                Description = request.Description,
                Note = request.Note,
                TargetAudience = request.TargetAudience,
                Prerequisites = request.Prerequisites,
                LanguageCode = request.LanguageCode,
                Category = request.Category,
                Price = request.Price,
                Currency = request.Currency,
            };
        }

        public static void UpdateLessonEntity(this Lesson lesson, UpdateLessonRequest request)
        {
            lesson.Name = request.Name ?? lesson.Name;
            lesson.Description = request.Description ?? lesson.Description;
            lesson.Note = request.Note ?? lesson.Note;
            lesson.TargetAudience = request.TargetAudience ?? lesson.TargetAudience;
            lesson.Prerequisites = request.Prerequisites ?? lesson.Prerequisites;
            lesson.LanguageCode = request.LanguageCode ?? lesson.LanguageCode;
            lesson.Category = request.Category ?? lesson.Category;
            lesson.Price = request.Price ?? lesson.Price;
            lesson.Currency = request.Currency ?? lesson.Currency;
        }
    }
    #endregion
}
