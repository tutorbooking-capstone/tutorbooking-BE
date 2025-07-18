using App.Core.Base;
using App.DTOs.PaymentDTOs;

namespace App.Services.Interfaces
{
    public interface IWalletService
    {
        // Wallet management
        Task<WalletResponse> GetWalletAsync(string? userId = null);
        Task<WalletResponse> GetSystemWalletAsync();
        Task<bool> CreateWalletIfNotExistsAsync(string userId);
        Task<bool> CreateWalletForAllUsersAsync();
        
        // Transaction history
        Task<BasePaginatedList<TransactionResponse>> GetTransactionsAsync(string? userId = null, int page = 1, int pageSize = 10);
        
        // Balance calculation
        Task<decimal> CalculateAvailableBalanceAsync(string walletId);
    }
}