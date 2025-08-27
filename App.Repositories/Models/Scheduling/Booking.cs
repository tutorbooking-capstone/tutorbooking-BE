using App.Core.Base;
using App.Core.Utils;
using App.Repositories.Models.Rating;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models
{
    public enum BookingStatus
    {
        [EnumDescription("Đang diễn ra")]
        Confirmed = 0,
        [EnumDescription("Đã yêu cầu khiếu nại")]
        DisputeRequested = 1,
        [EnumDescription("Đang tranh chấp")]
        Disputed = 2,
        [EnumDescription("Đã hủy")]
        Cancelled = 3
    }

    public class Booking : BaseEntity
    {
        public string TutorId { get; set; } = string.Empty;
        public string? LearnerId { get; set; }
        public string? Note { get; set; }
        public string? BookingSlotRatingId { get; set; }
        public string? LessonSnapshotId { get; set; }
        public string? OriginalOfferId { get; set; }
        public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
        
        public virtual Tutor? Tutor { get; set; }
        public virtual Learner? Learner { get; set; }
        public virtual ICollection<BookedSlot>? BookedSlots { get; set; }
        public virtual BookingSlotRating? BookingSlotRating { get; set; }
        public virtual LessonSnapshot? LessonSnapshot { get; set; }
        
        #region Behaviors
        public Expression<Func<Booking, object>>[] UpdateStatus(BookingStatus newStatus, string updatedBy)
        {
            if (Status == newStatus) return Array.Empty<Expression<Func<Booking, object>>>();
            
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

        // public Expression<Func<Booking, object>>[] SetCurrentDispute(string disputeId)
        // {
        //     if (CurrentDisputeId == disputeId) return Array.Empty<Expression<Func<Booking, object>>>();
            
        //     CurrentDisputeId = disputeId;
            
        //     return
        //     [
        //         x => x.CurrentDisputeId!
        //     ];
        // }
        
        // public Expression<Func<Booking, object>>[] ClearCurrentDispute()
        // {
        //     if (CurrentDisputeId == null) return Array.Empty<Expression<Func<Booking, object>>>();
            
        //     CurrentDisputeId = null;
            
        //     return
        //     [
        //         x => x.CurrentDisputeId!
        //     ];
        // }
        #endregion
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
