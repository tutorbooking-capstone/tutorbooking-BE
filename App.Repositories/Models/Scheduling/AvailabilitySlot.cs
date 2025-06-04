using App.Core.Base;

namespace App.Repositories.Models.Scheduling
{
    #region Enums 
    public enum SlotType
    {   
        Available = 0,    // Available for booking
        Unavailable = 1,  // Tutor marked as busy
        Booked = 2        // Booked by a learner or has notes
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
        public string? WeeklyPatternId { get; set; }

        public virtual WeeklyAvailabilityPattern? WeeklyPattern { get; set; }

        #region Behavior
        public static DateTime CalculateDateForDay(DateTime weekStart, DayInWeek dayInWeek)
        {
            int offset = dayInWeek switch
            {
                DayInWeek.Monday => 0,
                DayInWeek.Tuesday => 1,
                DayInWeek.Wednesday => 2,
                DayInWeek.Thursday => 3,
                DayInWeek.Friday => 4,
                DayInWeek.Saturday => 5,
                DayInWeek.Sunday => 6,
                _ => 0
            };

            return weekStart.AddDays(offset);
        }
        #endregion
    }
}
