using App.Core.Base;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using System.Linq.Expressions;
using System.Text.Json;

namespace App.Repositories.Models
{
    public class RequestedSlot : IEquatable<RequestedSlot>
    {
        public DayInWeek DayInWeek { get; set; }
        public int SlotIndex { get; set; }

        public bool Equals(RequestedSlot? other)
        {
            if (other is null) return false;
            return DayInWeek == other.DayInWeek && SlotIndex == other.SlotIndex;
        }

        public override bool Equals(object? obj)
            => Equals(obj as RequestedSlot);

        public override int GetHashCode()
            => HashCode.Combine(DayInWeek, SlotIndex);
    }

    public class LearnerTimeSlotRequest : CoreEntity
    {
        public string LearnerId { get; set; } = string.Empty;
        public string TutorId { get; set; } = string.Empty;
        public string? LessonId { get; set; }
        public DateTime ExpectedStartDate { get; set; }
        public string RequestedSlotsJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastViewedAt { get; set; }

        public virtual Learner Learner { get; set; } = null!;
        public virtual Tutor Tutor { get; set; } = null!;
        public virtual Lesson? Lesson { get; set; }

        #region Behavior
        public Expression<Func<LearnerTimeSlotRequest, object>>[] Update(
            string? lessonId,
            DateTime expectedStartDate,
            IEnumerable<RequestedSlot> slots)
        {
            LessonId = lessonId;
            ExpectedStartDate = expectedStartDate;
            RequestedSlotsJson = JsonSerializer.Serialize(slots.Distinct());
            CreatedAt = DateTime.UtcNow; // Treat update as a new "creation" time for the request
            LastViewedAt = null; // Reset viewed status on update

            return
            [
                x => x.LessonId!,
                x => x.ExpectedStartDate,
                x => x.RequestedSlotsJson,
                x => x.CreatedAt,
                x => x.LastViewedAt!
            ];
        }

        public static LearnerTimeSlotRequest Create(
            string learnerId,
            string tutorId,
            string? lessonId,
            DateTime expectedStartDate,
            IEnumerable<RequestedSlot> slots)
            => new()
            {
                LearnerId = learnerId,
                TutorId = tutorId,
                LessonId = lessonId,
                ExpectedStartDate = expectedStartDate,
                RequestedSlotsJson = JsonSerializer.Serialize(slots.Distinct()),
                CreatedAt = DateTime.UtcNow
            };

        public Expression<Func<LearnerTimeSlotRequest, object>>[] MarkAsViewed()
        {
            LastViewedAt = DateTime.UtcNow;
            return [x => x.LastViewedAt!];
        }
        #endregion
    }
}
