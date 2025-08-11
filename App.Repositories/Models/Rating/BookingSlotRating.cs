using App.Core.Base;
using App.Repositories.Models.User;
using System.ComponentModel.DataAnnotations.Schema;

namespace App.Repositories.Models.Rating
{
    public class BookingSlotRating: BaseEntity
    {
        [ForeignKey(nameof(Booking))]
        public string BookingId { get; set; }
        public string TutorId { get; set; }
        public string LearnerId { get; set; }
        public float TeachingQuality { get; set; } = 1;
        public float Attitude { get; set; } = 1;
        public float Commitment { get; set; } = 1;
        public string? Comment { get; set; }


        public virtual Booking? Booking { get; set; }
        public virtual Tutor? Tutor { get; set; }
        public virtual Learner? Learner { get; set; }
    }
}
