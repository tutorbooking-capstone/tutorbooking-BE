using App.Core.Base;
using App.Repositories.Models.User;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;

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

        public static Expression<Func<Tutor, double>> RatingSortExpression = e => e.BookingSlotRatings.Any() ?
                (e.BookingSlotRatings.Select(r => (r.TeachingQuality + r.Attitude + r.Commitment) / 3.0).Average() *
                Math.Min(e.BookingSlotRatings.Count / 10.0, 1.0)) + // Confidence factor
                (e.BookingSlotRatings.Select(r => (r.TeachingQuality + r.Attitude + r.Commitment) / 3.0).Average() * 0.3) // Base quality weight
                : 0;// Default score for tutors with no ratings


        public virtual Booking? Booking { get; set; }
        public virtual Tutor? Tutor { get; set; }
        public virtual Learner? Learner { get; set; }
    }
}
