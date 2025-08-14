using App.Core.Base;
using App.DTOs.BookingDTOs;

namespace App.Services.Interfaces
{
    public interface IDisputeService
    {
        // Learner operations
        Task<BookingDisputeResponse> CreateDisputeAsync(CreateDisputeRequest request);
        Task<BookingDisputeResponse> WithdrawDisputeAsync(WithdrawDisputeRequest request);
        Task<List<BookingDisputeResponse>> GetLearnerDisputesAsync(bool? onlyActive = null);
        Task<DisputeDetailResponse> GetDisputeDetailForLearnerAsync(string disputeId);
        
        // Tutor operations
        Task<BookingDisputeResponse> RespondToDisputeAsync(RespondToDisputeRequest request);
        Task<List<BookingDisputeResponse>> GetTutorDisputesAsync(bool? onlyActive = null);
        Task<DisputeDetailResponse> GetDisputeDetailForTutorAsync(string disputeId);
        
        // Staff operations
        Task<BookingDisputeResponse> ResolveDisputeAsync(ResolveDisputeRequest request);
        Task<List<BookingDisputeResponse>> GetDisputesForReviewAsync();
        Task<DisputeDetailResponse> GetDisputeDetailForStaffAsync(string disputeId);
        Task<BasePaginatedList<BookingDisputeResponse>> GetFilteredDisputesAsync(StaffDisputeFilterRequest filter);
        
        // System operations
        Task ProcessExpiredReconciliationsAsync();
        Task ProcessExpiredStaffReviewsAsync();
        Task<Dictionary<string, object>> GetDisputeMetadataAsync();
    }
}