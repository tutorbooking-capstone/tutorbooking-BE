using App.Repositories.Models;
using System.Linq.Expressions;

namespace App.DTOs.LessonDTOs
{
    public class LessonResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Note { get; set; }

        public string TargetAudience { get; set; } = string.Empty;
        public string Prerequisites { get; set; } = string.Empty;

        public string LanguageCode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        
        public decimal Price { get; set; }
        //public string Currency { get; set; } = string.Empty;
        public int DurationInMinutes { get; set; }
        public string TutorId { get; set; } = string.Empty;

        public static Expression<Func<Lesson, LessonResponse>> Projection
        => lesson => new LessonResponse
        {
            Id = lesson.Id,
            Name = lesson.Name,
            Description = lesson.Description,
            Note = lesson.Note,
            TargetAudience = lesson.TargetAudience,
            Prerequisites = lesson.Prerequisites,
            LanguageCode = lesson.LanguageCode,
            Category = lesson.Category,
            Price = lesson.Price,
            //Currency = lesson.Currency,
            DurationInMinutes = lesson.DurationInMinutes,
            TutorId = lesson.TutorId
        };
    }
}
