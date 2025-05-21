using App.Core.Base;

namespace App.Repositories.Models.Scheduling
{
    #region Enums 
    public enum SlotType
    {   
        Available,    // Available for booking
        Unavailable,  // Tutor marked as busy
        Booked        // Booked by a learner or has notes
    }

    public enum DayInWeek
    {
        Sunday = 1,
        Monday = 2,
        Tuesday = 3,
        Wednesday = 4,
        Thursday = 5,
        Friday = 6,
        Saturday = 7
    }
    #endregion

    public class AvailabilitySlot : CoreEntity
    {
        public SlotType Type { get; set; }
        public DayInWeek DayInWeek { get; set; }
        public int SlotIndex { get; set; } // Slot index within the day (e.g., 0 for 00:00-00:30, ..., 47 for 23:30-24:00)
        public string? BookingSlotId { get; set; }
        public string? WeeklyPatternId { get; set; }

        public virtual WeeklyAvailabilityPattern? WeeklyPattern { get; set; }
        public virtual BookingSlot? BookingSlot { get; set; } 
    }
}
