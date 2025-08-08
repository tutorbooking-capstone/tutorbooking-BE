using App.Core.Base;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models
{
    public class TutorBookingOffer : CoreEntity
    {
        public string TutorId { get; set; } = string.Empty;
        public string LearnerId { get; set; } = string.Empty;
        public string? LessonId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public TimeSpan ExpirationPeriod { get; set; } = TimeSpan.FromMinutes(30); // Mặc định 30 phút
        public bool IsRejected { get; set; } = false; 

        public virtual Tutor? Tutor { get; set; }
        public virtual Learner? Learner { get; set; }
        public virtual Lesson? Lesson { get; set; }
        public virtual ICollection<OfferedSlot> OfferedSlots { get; set; } = new List<OfferedSlot>();
        
        public bool IsExpired()
        {
            var referenceTime = UpdatedAt ?? CreatedAt;
            return DateTime.UtcNow > referenceTime.Add(ExpirationPeriod);
        }
        
        public DateTime GetExpirationTime()
        {
            var referenceTime = UpdatedAt ?? CreatedAt;
            return referenceTime.Add(ExpirationPeriod);
        }
        
        public Expression<Func<TutorBookingOffer, object>>[] MarkAsRejected()
        {
            if (IsRejected) return Array.Empty<Expression<Func<TutorBookingOffer, object>>>();
            
            IsRejected = true;
            UpdatedAt = DateTime.UtcNow;
            
            return
            [
                x => x.IsRejected,
                x => x.UpdatedAt!
            ];
        }
        
        #region Behavior
        public static TutorBookingOffer Create(
            string tutorId, 
            string learnerId, 
            string lessonId, 
            IEnumerable<(DateTime SlotDateTime, int SlotIndex)> offeredSlots)
        {
            var now = DateTime.UtcNow;
            
            return new TutorBookingOffer
            {
                TutorId = tutorId,
                LearnerId = learnerId,
                LessonId = lessonId,
                CreatedAt = now,
                ExpirationPeriod = TimeSpan.FromMinutes(30),
                OfferedSlots = offeredSlots.Select(s => new OfferedSlot
                {
                    SlotDateTime = s.SlotDateTime,
                    SlotIndex = s.SlotIndex,
                }).ToList()
            };
        }
        #endregion
    }
}