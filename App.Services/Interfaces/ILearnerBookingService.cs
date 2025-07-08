using App.DTOs.BookingDTOs;

namespace App.Services.Interfaces
{
    public interface ILearnerBookingService
    {
        Task UpdateTimeSlotRequestsAsync(LearnerTimeSlotRequestDTO request);
        Task DeleteTimeSlotRequestsAsync(string tutorId);
        Task<LearnerTimeSlotResponseDTO?> GetTimeSlotRequestByTutorAsync(string tutorId);
        Task<List<TutorInfoDTO>> GetAllTimeSlotRequestsForLearnerAsync();
        
        Task<List<TutorBookingOfferResponse>> GetBookingOffersForLearnerAsync();
        Task<TutorBookingOfferResponse> GetBookingOfferByIdForLearnerAsync(string offerId);
    }
}
