using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models
{
    public class TutorBookingOffer : CoreEntity
    {
        public string TutorId { get; set; } = string.Empty;
        public string LearnerId { get; set; } = string.Empty;
        public string? LessonId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public virtual Tutor? Tutor { get; set; }
        public virtual Learner? Learner { get; set; }
        public virtual Lesson? Lesson { get; set; }
        public virtual ICollection<OfferedSlot> OfferedSlots { get; set; } = new List<OfferedSlot>();
    }
}