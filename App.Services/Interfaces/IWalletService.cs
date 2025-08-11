using App.Core.Base;
using App.DTOs.PaymentDTOs;
using App.Repositories.Models;

namespace App.Services.Interfaces
{
    public interface IWalletService
    {
        // Wallet management
        Task<WalletResponse> GetWalletAsync(string? userId = null);
        Task<WalletResponse> GetSystemWalletAsync();
        Task<Wallet> GetEscrowWalletAsync();
        Task<bool> CreateWalletIfNotExistsAsync(string userId);
        Task<bool> CreateWalletForAllUsersAsync();
        
        // Transaction history
        Task<BasePaginatedList<TransactionResponse>> GetTransactionsAsync(string? userId = null, int page = 1, int pageSize = 10);
        
        // Balance calculation
        Task<decimal> CalculateAvailableBalanceAsync(string walletId);

        Task RefundHeldFundToLearnerAsync(string heldFundId);
        Task PartialRefundForDisputeAsync(string heldFundId, decimal tutorPercentage, string bookingId);
    }
}