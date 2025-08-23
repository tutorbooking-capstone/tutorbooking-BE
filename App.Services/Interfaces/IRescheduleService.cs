using App.Core.Base;
using App.DTOs.BookingDTOs;
using App.Repositories.Models;

namespace App.Services.Interfaces
{
    public interface IRescheduleService
    {
        Task<RescheduleRequestResponse> CreateRescheduleRequestAsync(CreateRescheduleRequest request);
        
        Task<BasePaginatedList<RescheduleRequestResponse>> GetRescheduleRequestsAsync(
            int pageIndex = 0, 
            int pageSize = 10, 
            RescheduleRequestStatus? status = null);
        
        Task<RescheduleRequestResponse> GetRescheduleRequestByIdAsync(string requestId);
        
        Task<RescheduleRequestResponse> AcceptRescheduleRequestAsync(string requestId);
        
        Task<RescheduleRequestResponse> RejectRescheduleRequestAsync(string requestId, string? note);
        
        Task<RescheduleRequestResponse> CancelRescheduleRequestAsync(string requestId);

        Task DeleteRescheduleRequestAsync(string requestId);
        
        Task<Dictionary<string, object>> GetRescheduleMetadataAsync();
    }
}
