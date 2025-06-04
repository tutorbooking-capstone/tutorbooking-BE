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

    public class BookedSlot : CoreEntity
    {
        public string BookingSlotId { get; set; } = string.Empty;
        public string AvailabilitySlotId { get; set; } = string.Empty;
        public DateTime BookedDate { get; set; }
        public string? SlotNote { get; set; } // Specific note for this particular booked slot (e.g. "Session will start 30 mins late")
        public SlotStatus Status { get; set; }

        public virtual BookingSlot? BookingSlot { get; set; }
        public virtual AvailabilitySlot? AvailabilitySlot { get; set; }
    }
}