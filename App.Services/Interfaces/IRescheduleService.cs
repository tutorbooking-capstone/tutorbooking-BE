using App.Core.Base;
using App.DTOs.BookingDTOs;

namespace App.Services.Interfaces
{
    public interface IRescheduleService
    {
        // Tutor requests to reschedule a slot
        Task<RescheduleRequestResponse> CreateRescheduleRequestAsync(CreateRescheduleRequest request);
        
        // Get reschedule requests for the authenticated user (either tutor or learner) with pagination
        Task<BasePaginatedList<RescheduleRequestResponse>> GetRescheduleRequestsAsync(int pageIndex = 0, int pageSize = 10);
        
        // Get a specific reschedule request by ID
        Task<RescheduleRequestResponse> GetRescheduleRequestByIdAsync(string requestId);
        
        // Learner accepts a reschedule request
        Task<RescheduleRequestResponse> AcceptRescheduleRequestAsync(string requestId);
        
        // Learner rejects a reschedule request
        Task<RescheduleRequestResponse> RejectRescheduleRequestAsync(string requestId, string? note);
        
        // Requester cancels their own pending reschedule request
        Task<RescheduleRequestResponse> CancelRescheduleRequestAsync(string requestId);
        
        Task<Dictionary<string, object>> GetRescheduleMetadataAsync();
    }
}
