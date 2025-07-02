using App.DTOs.BookingDTOs;

namespace App.Services.Interfaces
{
    public interface ITutorBookingService
    {
        Task<List<LearnerInfoDTO>> GetAllTimeSlotRequestsForTutorAsync();
        Task<List<LearnerTimeSlotResponseDTO>> GetTimeSlotRequestsByLearnerAsync(string learnerId);
        
        Task<TutorBookingOfferResponse> CreateBookingOfferAsync(CreateTutorBookingOfferRequest request);
        Task<List<TutorBookingOfferResponse>> GetAllBookingOffersByTutorAsync();
        Task<TutorBookingOfferResponse> GetBookingOfferByIdForTutorAsync(string offerId);
        Task<TutorBookingOfferResponse> UpdateBookingOfferAsync(string offerId, UpdateTutorBookingOfferRequest request);
        Task DeleteBookingOfferAsync(string offerId);
    }
}