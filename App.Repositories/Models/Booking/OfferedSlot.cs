using App.Core.Base;

namespace App.Repositories.Models.Booking
{
    public class OfferedSlot : CoreEntity
    {
        public string TutorBookingOfferId { get; set; } = string.Empty;
        public DateTime SlotDateTime { get; set; }
        public int SlotIndex { get; set; }
        //public decimal Price { get; set; }

        public virtual TutorBookingOffer? TutorBookingOffer { get; set; }

        #region Behavior
        public static OfferedSlot Create(string tutorBookingOfferId, DateTime slotDateTime, int slotIndex)
        => new OfferedSlot
        {
            TutorBookingOfferId = tutorBookingOfferId,
            SlotDateTime = slotDateTime,
            SlotIndex = slotIndex
        };
        #endregion
    }
}