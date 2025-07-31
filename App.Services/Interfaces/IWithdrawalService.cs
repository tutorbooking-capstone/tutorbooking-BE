using App.Core.Base;
using App.DTOs.PaymentDTOs;
using App.Repositories.Models;

namespace App.Services.Interfaces
{
    public interface IWithdrawalService
    {
        Task<WithdrawalRequestResponse> CreateWithdrawalRequestAsync(CreateWithdrawalRequest request);
        Task<BasePaginatedList<WithdrawalRequestResponse>> GetWithdrawalRequestsAsync(
            int page = 1, 
            int pageSize = 10, 
            WithdrawalRequestStatus? status = null);
        Task<WithdrawalRequestResponse> GetWithdrawalRequestByIdAsync(string requestId);
        Task<WithdrawalRequestResponse> ProcessWithdrawalAsync(ProcessWithdrawalRequest request);
        Task<WithdrawalRequestResponse> RejectWithdrawalAsync(RejectWithdrawalRequest request);
        Task<Dictionary<string, object>> GetWithdrawalMetadataAsync();
    }
}