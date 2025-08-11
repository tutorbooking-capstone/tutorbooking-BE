using App.Repositories.Models.Rating;

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
                BookingSlotId = entity.BookingId,
                TeachingQuality = entity.TeachingQuality,
                Attitude = entity.Attitude,
                Commitment = entity.Commitment,
                Comment = entity.Comment,
            };
        }
    }
}
