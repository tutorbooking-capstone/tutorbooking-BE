using App.DTOs.BookingDTOs;

namespace App.Services.Interfaces
{
    public interface ITutorBookingService
    {
        Task<List<LearnerInfoDTO>> GetAllTimeSlotRequestsForTutorAsync();
        Task<List<LearnerTimeSlotResponseDTO>> GetTimeSlotRequestsByLearnerAsync(string learnerId);
    }
}