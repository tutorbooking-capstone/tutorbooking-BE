using App.Core.Base;
using App.Repositories.Models.Scheduling;
using System.Linq.Expressions;

namespace App.Repositories.Models
{
    #region Enums
    public enum HeldFundStatus
    {
        Held = 0,
        ReleasedToTutor = 1,
        RefundedToLearner = 2,
        Disputed = 3,
        ReleasedToTutorBank = 4
    }

        public enum HeldFundType
    {
        BookingPayment = 0,
        Withdrawal = 1
    }
    #endregion
    
    public class HeldFund : CoreEntity
    {
        public string? BookedSlotId { get; set; } 
        public string? WithdrawalRequestId { get; set; }  
        public decimal Amount { get; set; }
        public HeldFundStatus Status { get; set; } = HeldFundStatus.Held;
        public DateTime? ReleaseAt { get; set; } 
        public DateTime? ResolvedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public HeldFundType Type { get; set; } = HeldFundType.BookingPayment; 

        public virtual BookedSlot? BookedSlot { get; set; }
        public virtual WithdrawalRequest? WithdrawalRequest { get; set; }
        
        public static HeldFund CreateForWithdrawal(string withdrawalRequestId, decimal amount)
        {
            return new HeldFund
            {
                WithdrawalRequestId = withdrawalRequestId,
                Amount = amount,
                Status = HeldFundStatus.Held,
                Type = HeldFundType.Withdrawal,
                ReleaseAt = null 
            };
        }
        
        public static HeldFund CreateForBooking(string bookedSlotId, decimal amount, DateTime releaseAt)
        {
            return new HeldFund
            {
                BookedSlotId = bookedSlotId,
                Amount = amount,
                Status = HeldFundStatus.Held,
                Type = HeldFundType.BookingPayment,
                ReleaseAt = releaseAt
            };
        }
        
        public Expression<Func<HeldFund, object>>[] UpdateStatus(HeldFundStatus newStatus)
        {
            var updated = new List<Expression<Func<HeldFund, object>>>();
            
            if (Status != newStatus)
            {
                Status = newStatus;
                updated.Add(x => x.Status);
                
                if (newStatus != HeldFundStatus.Held && ResolvedAt == null)
                {
                    ResolvedAt = DateTime.UtcNow;
                    updated.Add(x => x.ResolvedAt!);
                }
            }
            
            return updated.ToArray();
        }
    }




}