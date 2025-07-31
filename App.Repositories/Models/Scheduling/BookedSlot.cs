using App.Core.Base;
using App.Core.Utils;
using System.Linq.Expressions;

namespace App.Repositories.Models.Scheduling
{
    #region Enums
    public enum SlotStatus
    {
        Pending = 0,        // Pending
        AwaitingConfirmation = 1, // Awaiting Confirmation
        Completed = 2,      // Completed
        Cancelled = 3       // Cancelled
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

        public virtual Booking? Booking { get; set; }
        public virtual HeldFund? HeldFund { get; set; }

        #region Behaviors
        public Expression<Func<BookedSlot, object>>[] MarkAsCompleted(string updatedBy)
        {
            if (Status == SlotStatus.Completed) return Array.Empty<Expression<Func<BookedSlot, object>>>();
            if (Status == SlotStatus.Cancelled)
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
        #endregion
    }
}