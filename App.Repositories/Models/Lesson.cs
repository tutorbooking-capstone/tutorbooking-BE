using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models
{
    public class Lesson : CoreEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Note { get; set; }

        public string TargetAudience { get; set; } = string.Empty; // Who the lesson is aimed at (e.g. "Beginner", "Intermediate", "Advanced")
        public string Prerequisites { get; set; } = string.Empty; // Required knowledge/skills before taking the lesson

        public string LanguageCode { get; set; } = string.Empty; // Base on ISO Code
        public string Category { get; set; } = string.Empty;

        public decimal Price { get; set; }
        public string Currency { get; set; } = "VND";
        public int DurationInMinutes { get; set; } = 30;

        public string TutorId { get; set; } = string.Empty;
        public virtual Tutor? Tutor { get; set; }
    }
}