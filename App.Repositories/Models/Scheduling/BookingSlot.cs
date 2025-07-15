using App.Core.Base;
using App.Repositories.Models.Rating;
using App.Repositories.Models.User;

namespace App.Repositories.Models.Scheduling
{
    public class BookingSlot : CoreEntity
    {
        public string TutorId { get; set; } = string.Empty;
        public string? LearnerId { get; set; }
        public string? Note { get; set; } // General note for the entire booking (e.g. Google Meet link for all sessions)
        public string? BookingSlotRatingId { get; set; }

        public virtual Tutor? Tutor { get; set; }
        public virtual Learner? Learner { get; set; }
        public virtual ICollection<BookedSlot>? BookedSlots { get; set; }
        public virtual BookingSlotRating? BookingSlotRating { get; set; }
    }
}



        // #region Behavior
        // public int TotalOccurrences => (RepeatForWeeks ?? 0) + 1;

        // public bool OccursOn(DateTime date)
        // {
        //     var daysDiff = (date.Date - StartDate.Date).Days;
        //     if (daysDiff < 0) return false;

        //     if (RepeatForWeeks == null || RepeatForWeeks == 0)
        //         return date.Date == StartDate.Date;

        //     if (daysDiff % 7 == 0)
        //     {
        //         var weekIndex = daysDiff / 7;
        //         return weekIndex >= 0 && weekIndex < TotalOccurrences; // weekIndex is 0-based
        //     }
        //     return false;
        // }

        // public bool IsSlotBookedOn(DateTime date, DayInWeek dayInWeek, int slotIndex)
        // {
        //     if (!OccursOn(date)) return false;
        //     return Slots != null && Slots.Any(s => 
        //         s.DayInWeek == dayInWeek 
        //         && s.SlotIndex == slotIndex 
        //         && s.BookingSlotId == Id);
        // }
        // #endregion