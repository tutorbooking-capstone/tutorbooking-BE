using App.DTOs.RatingDTOs;
using App.Repositories.Models.Rating;

namespace App.Services.Interfaces
{
    public interface IBookingSlotRatingService
    {
        Task<BookingSlotRating> CreateAsync(BookingSlotRatingRequest request);
        Task DeleteAsync(string id);
        Task<BookingSlotRating> GetByIdAsync(string id);
        Task<TutorRatingResponse> GetTutorRatingAsync(string tutorId);
        Task UpdateAsync(BookingSlotRatingUpdateRequest request);
    }
}