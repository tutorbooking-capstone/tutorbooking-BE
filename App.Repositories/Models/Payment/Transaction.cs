using App.Core.Base;

namespace App.Repositories.Models
{
    #region Enums
    public enum TransactionType
    {
        Deposit = 0,
        Withdrawal = 1,
        Payment = 2,
        Refund = 3,
        Commission = 4,
        Fee = 5
    }

    public enum TransactionStatus
    {
        Pending = 0,
        Success = 1,
        Failed = 2
    }
    #endregion

    public class Transaction : CoreEntity  // Sử dụng CoreEntity vì giao dịch không thể sửa đổi
    {
        public Transaction()
        {
            CreatedAt = DateTime.UtcNow;
        }
        
        public string? SourceWalletId { get; set; }  // NULL cho nạp tiền từ bên ngoài
        public string? TargetWalletId { get; set; }  // NULL cho rút tiền ra ngân hàng
        public decimal Amount { get; set; }
        public TransactionType Type { get; set; }
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
        public string? ReferenceId { get; set; }  // Liên kết đến HeldFund, DepositRequest, WithdrawalRequest
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        
        public virtual Wallet? SourceWallet { get; set; }
        public virtual Wallet? TargetWallet { get; set; }
        
        // Các phương thức factory
        public static Transaction CreateDepositTransaction(string targetWalletId, decimal amount, string referenceId)
        {
            return new Transaction
            {
                SourceWalletId = null,
                TargetWalletId = targetWalletId,
                Amount = amount,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Success,
                ReferenceId = referenceId,
                Description = $"Nạp {amount} VND vào ví"
            };
        }
        
        public static Transaction CreateWithdrawalTransaction(string sourceWalletId, decimal amount, string referenceId)
        {
            return new Transaction
            {
                SourceWalletId = sourceWalletId,
                TargetWalletId = null,
                Amount = amount,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Pending,
                ReferenceId = referenceId,
                Description = $"Rút {amount} VND từ ví"
            };
        }
        
        public static Transaction CreatePaymentTransaction(string sourceWalletId, decimal amount, string referenceId, string description)
        {
            return new Transaction
            {
                SourceWalletId = sourceWalletId,
                TargetWalletId = null,  // Giữ trong escrow
                Amount = amount,
                Type = TransactionType.Payment,
                Status = TransactionStatus.Success,
                ReferenceId = referenceId,
                Description = description
            };
        }
    }

}