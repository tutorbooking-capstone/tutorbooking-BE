using App.Core.Base;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models
{
    #region Enums
    public enum DepositRequestStatus
    {
        Pending = 0,
        Success = 1,
        Failed = 2
    }
    #endregion

    public class DepositRequest : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentGateway { get; set; } = "PayOS";  // Mặc định là PayOS
        public string? GatewayTransactionId { get; set; }
        public DepositRequestStatus Status { get; set; } = DepositRequestStatus.Pending;
        public DateTime? CompletedAt { get; set; }
        
        // Các trường đặc thù cho PayOS
        public string? PayosOrderUrl { get; set; }
        public string? PayosOrderToken { get; set; }
        public string? PayosQrCode { get; set; }
        
        public virtual AppUser? User { get; set; }
        
        public Expression<Func<DepositRequest, object>>[] Complete(string gatewayTransactionId)
        {
            if (Status != DepositRequestStatus.Pending)
                return Array.Empty<Expression<Func<DepositRequest, object>>>();
                
            GatewayTransactionId = gatewayTransactionId;
            Status = DepositRequestStatus.Success;
            CompletedAt = DateTime.UtcNow;
            
            return 
            [
                x => x.GatewayTransactionId!,
                x => x.Status,
                x => x.CompletedAt!
            ];
        }
        
        public Expression<Func<DepositRequest, object>>[] MarkAsFailed()
        {
            if (Status != DepositRequestStatus.Pending)
                return Array.Empty<Expression<Func<DepositRequest, object>>>();
                
            Status = DepositRequestStatus.Failed;
            CompletedAt = DateTime.UtcNow;
            
            return 
            [
                x => x.Status,
                x => x.CompletedAt!
            ];
        }
    }


}