using App.Core.Base;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models
{
    public class LearnerTimeSlotRequest : CoreEntity
    {
        public string LearnerId { get; set; } = string.Empty;
        public string TutorId { get; set; } = string.Empty;
        public DayInWeek DayInWeek { get; set; }
        public int SlotIndex { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastViewedAt { get; set; }

        public virtual Learner Learner { get; set; } = null!;
        public virtual Tutor Tutor { get; set; } = null!;

        #region Behavior
        public static LearnerTimeSlotRequest Create(
            string learnerId, 
            string tutorId, 
            DayInWeek dayInWeek, 
            int slotIndex)
            => new LearnerTimeSlotRequest
            {
                LearnerId = learnerId,
                TutorId = tutorId,
                DayInWeek = dayInWeek,
                SlotIndex = slotIndex,
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
