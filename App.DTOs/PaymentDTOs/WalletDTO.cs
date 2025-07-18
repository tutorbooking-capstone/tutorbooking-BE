
using App.Repositories.Models;

namespace App.DTOs.PaymentDTOs
{
    public class WalletResponse
    {
        public string Id { get; set; } = string.Empty;
        public string? UserId { get; set; }
        public WalletType Type { get; set; }
        public decimal Balance { get; set; }
        public decimal AvailableBalance { get; set; }
        public string Currency { get; set; } = string.Empty;
        public WalletStatus Status { get; set; }
        public string UserFullName { get; set; } = string.Empty;

        public static WalletResponse FromEntity(Wallet wallet, decimal availableBalance)
        {
            return new WalletResponse
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                Type = wallet.Type,
                Balance = wallet.Balance,
                AvailableBalance = availableBalance,
                Currency = wallet.Currency,
                Status = wallet.Status,
                UserFullName = wallet.Type == WalletType.System 
                    ? "Hệ thống" 
                    : wallet.User?.FullName ?? string.Empty
            };
        }
    }

    public class TransactionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string? SourceWalletId { get; set; }
        public string? TargetWalletId { get; set; }
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public TransactionStatus Status { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string? SourceWalletOwner { get; set; }
        public string? TargetWalletOwner { get; set; }
    }
}