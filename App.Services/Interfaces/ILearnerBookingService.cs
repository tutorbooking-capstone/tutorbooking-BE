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
        
        Task<BookingResponse> AcceptTutorOfferAsync(AcceptOfferRequest request);
        Task<TutorBookingOfferResponse> RejectBookingOfferAsync(string offerId);
        Task<BookingResponse> CreateInstantBookingAsync(InstantBookingRequest request);
        Task<BookingResponse> CancelBookingAsync(string bookingId, string? cancellationReason = null);
    }
}
