using App.Repositories.Models.Rating;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.RatingDTOs
{
    public class BookingSlotRatingResponse
    {
        public string Id { get; set; }
        public string BookingSlotId { get; set; }
        public float TeachingQuality { get; set; } = 1;
        public float Attitude { get; set; } = 1;
        public float Commitment { get; set; } = 1;
        public string? Comment { get; set; }
    }

    public static class BookingSlotRatingResponseExtensions
    {
        public static BookingSlotRatingResponse ToResponse(this BookingSlotRating entity)
        {
            return new BookingSlotRatingResponse()
            {
                Id = entity.Id,
                BookingSlotId = entity.BookingSlotId,
                TeachingQuality = entity.TeachingQuality,
                Attitude = entity.Attitude,
                Commitment = entity.Commitment,
                Comment = entity.Comment,
            };
        }
    }
}
