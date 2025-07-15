using App.Repositories.Models.Rating;
using App.Repositories.Models.User;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.RatingDTOs
{
    public class BookingSlotRatingRequest
    {
        public string BookingSlotId { get; set; }
        public float TeachingQuality { get; set; }
        public float Attitude { get; set; }
        public float Commitment { get; set; } 
        public string? Comment { get; set; }
    }
    public class BookedSlotRatingRequestValidator : AbstractValidator<BookingSlotRatingRequest>
    {
        public BookedSlotRatingRequestValidator()
        {
            RuleFor(r => r.BookingSlotId).NotEmpty()
                .WithMessage("BOOKEDSLOTID_REQUIRED");
            RuleFor(r => r.TeachingQuality).InclusiveBetween(1, 5)
                .WithMessage("TEACHINGQUALITY_BETWEEN_1_AND_5");
            RuleFor(r => r.Attitude).InclusiveBetween(1, 5)
                .WithMessage("ATTITUDE_BETWEEN_1_AND_5");
            RuleFor(r => r.Commitment).InclusiveBetween(1, 5)
                .WithMessage("COMMITMENT_BETWEEN_1_AND_5");
        }
    }

    public static class BookingSlotRatingRequestExtensions
    {
        public static BookingSlotRating ToEntity(this BookingSlotRatingRequest request, string tutorId, string LearnerId)
        {
            return new BookingSlotRating()
            {
                BookingSlotId = request.BookingSlotId,
                TutorId = tutorId,
                LearnerId = LearnerId,
                TeachingQuality = request.TeachingQuality,
                Attitude = request.Attitude,
                Commitment = request.Commitment,
                Comment = request.Comment,
            };
        }
    }
}
