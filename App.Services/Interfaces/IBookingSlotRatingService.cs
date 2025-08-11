using App.DTOs.RatingDTOs;
using App.Repositories.Models.Rating;

namespace App.Services.Interfaces
{
    public interface IBookingSlotRatingService
    {
        Task<BookingSlotRating> CreateAsync(BookingSlotRatingRequest request);
        Task DeleteAsync(string id);
        Task<BookingSlotRating?> GetByBookingIdAsync(string bookingSlotId);
        Task<BookingSlotRating> GetByIdAsync(string id);
        Task<TutorRatingResponse?> GetTutorRatingAsync(string tutorId, int page = 1, int size = 10);
        Task UpdateAsync(BookingSlotRatingUpdateRequest request);
    }
}