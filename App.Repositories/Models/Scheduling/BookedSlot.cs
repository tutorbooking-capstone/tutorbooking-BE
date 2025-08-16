using App.Core.Base;
using App.Core.Utils;
using System.Linq.Expressions;

namespace App.Repositories.Models.Scheduling
{
    #region Enums
    public enum SlotStatus
    {
        [EnumDescription("Đang chờ diễn ra")]
        Pending = 0,
        
        [EnumDescription("Đang chờ thanh toán cho gia sư")]
        AwaitingPayout = 1,
        
        [EnumDescription("Đã hoàn thành")]
        Completed = 2,
        
        [EnumDescription("Đã hủy")]
        Cancelled = 3,
        
        [EnumDescription("Đã hủy do tranh chấp")]
        CancelledDisputed = 4

        // [EnumDescription("Đang chờ thanh toán cho gia sư")]
        // AwaitingPayment = 5
    }
    #endregion

    public class BookedSlot : BaseEntity
    {
        public string BookingId { get; set; } = string.Empty;
        public DateTime BookedDate { get; set; }
        public int SlotIndex { get; set; }  // Added SlotIndex
        public string? SlotNote { get; set; } // Specific note for this particular booked slot (e.g. "Session will start 30 mins late")
        public SlotStatus Status { get; set; }
        public string? HeldFundId { get; set; }  // Reference to held fund
        public string? DisputeId { get; set; }   // Reference to dispute causing cancellation

        public virtual Booking? Booking { get; set; }
        public virtual HeldFund? HeldFund { get; set; }
        public virtual BookingDispute? Dispute { get; set; }
        public virtual ICollection<RescheduleRequest> RescheduleRequests { get; set; } = new List<RescheduleRequest>();

        #region Behaviors
        public Expression<Func<BookedSlot, object>>[] MarkAsCompleted(string updatedBy)
        {
            if (Status == SlotStatus.Completed) return Array.Empty<Expression<Func<BookedSlot, object>>>();
            if (Status == SlotStatus.Cancelled || Status == SlotStatus.CancelledDisputed)
                throw new InvalidOperationException("Cannot complete a slot that has been cancelled.");

            Status = SlotStatus.Completed;
            LastUpdatedBy = updatedBy;
            LastUpdatedTime = CoreHelper.SystemTimeNow;

            return
            [
                x => x.Status,
                x => x.LastUpdatedBy!,
                x => x.LastUpdatedTime
            ];
        }

        public Expression<Func<BookedSlot, object>>[] MarkAsCancelled(string updatedBy)
        {
            if (Status == SlotStatus.Cancelled) return Array.Empty<Expression<Func<BookedSlot, object>>>();
            if (Status == SlotStatus.Completed)
                throw new InvalidOperationException("Cannot cancel a slot that has been completed.");
            
            Status = SlotStatus.Cancelled;
            LastUpdatedBy = updatedBy;
            LastUpdatedTime = CoreHelper.SystemTimeNow;

            return
            [
                x => x.Status,
                x => x.LastUpdatedBy!,
                x => x.LastUpdatedTime
            ];
        }

        public Expression<Func<BookedSlot, object>>[] MarkAsCancelledDisputed(string disputeId, string updatedBy)
        {
            if (Status == SlotStatus.CancelledDisputed) return Array.Empty<Expression<Func<BookedSlot, object>>>();
            if (Status == SlotStatus.Completed)
                throw new InvalidOperationException("Cannot cancel a slot that has been completed.");
            
            Status = SlotStatus.CancelledDisputed;
            DisputeId = disputeId;
            LastUpdatedBy = updatedBy;
            LastUpdatedTime = CoreHelper.SystemTimeNow;

            return
            [
                x => x.Status,
                x => x.DisputeId!,
                x => x.LastUpdatedBy!,
                x => x.LastUpdatedTime
            ];
        }

        public Expression<Func<BookedSlot, object>>[] UpdateStatus(SlotStatus newStatus, string updatedBy)
        {
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

        public bool IsUpcoming()
        {
            // A slot is upcoming if it's in the future and not cancelled
            return BookedDate > DateTime.UtcNow && 
                    Status != SlotStatus.Cancelled && 
                    Status != SlotStatus.CancelledDisputed;
        }

        public BookedSlot RescheduleToNewSlot(DateTime newDateTime, int newSlotIndex, string updatedBy)
        {
            var newSlot = new BookedSlot
            {
                BookingId = this.BookingId,
                BookedDate = newDateTime,
                SlotIndex = newSlotIndex,
                Status = SlotStatus.Pending,
                SlotNote = this.SlotNote,
                HeldFundId = this.HeldFundId,
                CreatedBy = updatedBy,
                CreatedTime = CoreHelper.SystemTimeNow,
                LastUpdatedBy = updatedBy,
                LastUpdatedTime = CoreHelper.SystemTimeNow
            };
            
            return newSlot;
        }
        #endregion
    }
}