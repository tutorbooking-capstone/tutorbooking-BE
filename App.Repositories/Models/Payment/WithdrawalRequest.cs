using App.Core.Base;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models
{
    #region Enums
    public enum WithdrawalRequestStatus
    {
        Pending = 0,
        Processing = 1,
        Completed = 2,
        Failed = 3
    }
    #endregion

    public class WithdrawalRequest : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string BankAccountId { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public string Fees { get; set; } = "{}"; // Định dạng JSON
        public decimal NetAmount { get; set; }
        public WithdrawalRequestStatus Status { get; set; } = WithdrawalRequestStatus.Pending;
        public DateTime? CompletedAt { get; set; }
        public string? RejectionReason { get; set; }
        
        public virtual AppUser? User { get; set; }
        public virtual BankAccount? BankAccount { get; set; }
        
        public Expression<Func<WithdrawalRequest, object>>[] Process()
        {
            if (Status != WithdrawalRequestStatus.Pending)
                return Array.Empty<Expression<Func<WithdrawalRequest, object>>>();
                
            Status = WithdrawalRequestStatus.Processing;
            
            return [x => x.Status];
        }
        
        public Expression<Func<WithdrawalRequest, object>>[] Complete()
        {
            if (Status != WithdrawalRequestStatus.Processing && Status != WithdrawalRequestStatus.Pending)
                return Array.Empty<Expression<Func<WithdrawalRequest, object>>>();
                
            Status = WithdrawalRequestStatus.Completed;
            CompletedAt = DateTime.UtcNow;
            
            return 
            [
                x => x.Status,
                x => x.CompletedAt!
            ];
        }
        
        public Expression<Func<WithdrawalRequest, object>>[] Reject(string reason)
        {
            if (Status != WithdrawalRequestStatus.Pending && Status != WithdrawalRequestStatus.Processing)
                return Array.Empty<Expression<Func<WithdrawalRequest, object>>>();
                
            Status = WithdrawalRequestStatus.Failed;
            RejectionReason = reason;
            CompletedAt = DateTime.UtcNow;
            
            return 
            [
                x => x.Status,
                x => x.RejectionReason!,
                x => x.CompletedAt!
            ];
        }
    }

}