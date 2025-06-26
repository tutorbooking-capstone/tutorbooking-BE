using App.DTOs.BookingDTOs;

namespace App.Services.Interfaces
{
    public interface ILearnerBookingService
    {
        Task UpdateTimeSlotRequestsAsync(LearnerTimeSlotRequestDTO request);
        Task DeleteTimeSlotRequestsAsync(string tutorId);
        Task<List<LearnerTimeSlotResponseDTO>> GetTimeSlotRequestsByTutorAsync(string tutorId);
    }
}
