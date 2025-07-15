using App.Repositories.Models.Rating;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.RatingDTOs
{
    public class BookingSlotRatingUpdateRequest
    {
        public string Id { get; set; }
        public float TeachingQuality { get; set; }
        public float Attitude { get; set; }
        public float Commitment { get; set; }
        public string? Comment { get; set; }
    }

    public class BookedSlotRatingUpdateRequestValidator : AbstractValidator<BookingSlotRatingUpdateRequest>
    {
        public BookedSlotRatingUpdateRequestValidator()
        {
            RuleFor(r => r.Id).NotEmpty()
                .WithMessage("ID_REQUIRED");
            RuleFor(r => r.TeachingQuality).InclusiveBetween(1, 5)
                .WithMessage("TEACHINGQUALITY_BETWEEN_1_AND_5");
            RuleFor(r => r.Attitude).InclusiveBetween(1, 5)
                .WithMessage("ATTITUDE_BETWEEN_1_AND_5");
            RuleFor(r => r.Commitment).InclusiveBetween(1, 5)
                .WithMessage("COMMITMENT_BETWEEN_1_AND_5");
        }
    }

    public static class BookingSlotRatingUpdateRequestExtenstions
    {
        public static void UpdateEntity(this BookingSlotRatingUpdateRequest request, ref BookingSlotRating entity)
        {
            entity.TeachingQuality = request.TeachingQuality;
            entity.Attitude = request.Attitude;
            entity.Commitment = request.Commitment;
            entity.Comment = request.Comment;
        }
    }
}
