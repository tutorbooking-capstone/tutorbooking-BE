using App.Core.Base;

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
    }
}