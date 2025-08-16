using App.Core.Base;

namespace App.Repositories.Models.Scheduling
{
    #region Enums 
    public enum SlotType
    {   
        [EnumDescription("Có thể đặt lịch")]
        Available = 0,    // Slot is available for booking
        
        [EnumDescription("Tạm giữ")]
        OnHold = 1,       // Slot is temporarily held (e.g., pending confirmation)
        
        [EnumDescription("Đã đặt lịch")] 
        Booked = 2        // Slot is booked by a learner
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
        // public SlotType Type { get; set; }
        public DayInWeek DayInWeek { get; set; }

        // Slot index within the day (e.g., 0 for 00:00-00:30, ..., 47 for 23:30-24:00)
        public int SlotIndex { get; set; } 
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

        public static AvailabilitySlot Create(DayInWeek dayInWeek, int slotIndex)
        {
            return new AvailabilitySlot
            {
                DayInWeek = dayInWeek,
                SlotIndex = slotIndex
            };
        }

        // Đã đơn giản hóa phương thức này
        public static AvailabilitySlot CreateAvailable(DayInWeek dayInWeek, int slotIndex)
            => Create(dayInWeek, slotIndex);

        public bool IsValid()
        {
            // Validate SlotIndex (0-47 cho 48 slot 30 phút trong ngày)
            if (SlotIndex < 0 || SlotIndex > 47)
                return false;

            // Validate DayInWeek (phải là giá trị hợp lệ trong enum)
            if (!Enum.IsDefined(typeof(DayInWeek), DayInWeek))
                return false;

            return true;
        }

        public TimeSpan GetStartTime()
        {
            return TimeSpan.FromMinutes(SlotIndex * 30);
        }

        public TimeSpan GetEndTime()
        {
            return TimeSpan.FromMinutes((SlotIndex + 1) * 30);
        }
        #endregion
    }
}
