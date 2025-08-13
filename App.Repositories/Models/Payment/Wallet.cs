using App.Core.Base;
using App.Core.Utils;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models
{
    #region Enums
    public enum WalletType
    {
        [EnumDescription("Ví cá nhân của người dùng")]
        Personal = 0,
        [EnumDescription("Ví hệ thống dùng để lưu trữ phí và doanh thu")]
        System = 1,
        [EnumDescription("Ví ký quỹ dùng để giữ tiền tạm thời")]
        Escrow = 2
    }

    public enum WalletStatus
    {
        [EnumDescription("Ví đang hoạt động bình thường")]
        Active = 0,
        [EnumDescription("Ví đã bị khóa, không thể thực hiện giao dịch")]
        Locked = 1
    }
    #endregion

    public class Wallet : BaseEntity
    {
        public string? UserId { get; set; }  // NULL cho ví hệ thống
        public WalletType Type { get; set; } = WalletType.Personal;
        public decimal Balance { get; set; } = 0;
        public string Currency { get; set; } = "VND";
        public WalletStatus Status { get; set; } = WalletStatus.Active;
        
        public virtual AppUser? User { get; set; }
        public virtual ICollection<Transaction> SourceTransactions { get; set; } = new List<Transaction>();
        public virtual ICollection<Transaction> TargetTransactions { get; set; } = new List<Transaction>();

        #region Behaviors
        public bool CanWithdraw(decimal amount) => Balance >= amount && Status == WalletStatus.Active;
        
        public Expression<Func<Wallet, object>>[] UpdateBalance(decimal newBalance)
        {
            if (Balance == newBalance) return Array.Empty<Expression<Func<Wallet, object>>>();
            
            Balance = newBalance;
            return [x => x.Balance];
        }
        
        public Expression<Func<Wallet, object>>[] AddBalance(decimal amount)
        {
            if (amount == 0) return Array.Empty<Expression<Func<Wallet, object>>>();
            
            Balance += amount;
            return [x => x.Balance];
        }
        
        public Expression<Func<Wallet, object>>[] SubtractBalance(decimal amount)
        {
            if (amount <= 0) return Array.Empty<Expression<Func<Wallet, object>>>();
            if (Balance < amount) throw new InvalidOperationException($"Insufficient balance. Current: {Balance}, Requested: {amount}");
            
            Balance -= amount;
            return [x => x.Balance];
        }
        
        public Expression<Func<Wallet, object>>[] UpdateStatus(WalletStatus newStatus, string? updatedBy = null)
        {
            if (Status == newStatus) return Array.Empty<Expression<Func<Wallet, object>>>();
            
            Status = newStatus;
            LastUpdatedBy = updatedBy;
            LastUpdatedTime = CoreHelper.SystemTimeNow;
            
            return 
            [
                x => x.Status,
                x => x.LastUpdatedBy!,
                x => x.LastUpdatedTime
            ];
        }
        #endregion
    }

}