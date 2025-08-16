using App.Core.Base;

namespace App.Repositories.Models
{
    public class OfferedSlot : CoreEntity
    {
        public string? TutorBookingOfferId { get; set; } 
        public string? RescheduleRequestId { get; set; }
        
        public DateTime SlotDateTime { get; set; }
        public int SlotIndex { get; set; }
        public bool IsForReschedule { get; set; } = false;
        
        // Navigation properties
        public virtual TutorBookingOffer? TutorBookingOffer { get; set; }
        public virtual RescheduleRequest? RescheduleRequest { get; set; }

        #region Behavior
        public static OfferedSlot Create(string tutorBookingOfferId, DateTime slotDateTime, int slotIndex)
        => new OfferedSlot
        {
            TutorBookingOfferId = tutorBookingOfferId,
            SlotDateTime = slotDateTime,
            SlotIndex = slotIndex
        };
        
        public static OfferedSlot CreateForReschedule(string rescheduleRequestId, DateTime slotDateTime, int slotIndex)
        => new OfferedSlot
        {
            RescheduleRequestId = rescheduleRequestId,
            SlotDateTime = slotDateTime,
            SlotIndex = slotIndex
        };
        #endregion
    }
}