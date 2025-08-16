using App.Core.Base;
using App.Repositories.Models.User;
using System.Linq.Expressions;

namespace App.Repositories.Models.Scheduling
{
    public class BookingConfig : CoreEntity
    {
        public string TutorId { get; set; } = string.Empty;
        
        public bool AllowInstantBooking { get; set; } = false;
        public int MaxInstantBookingSlots { get; set; } = 1;
        
        public virtual Tutor Tutor { get; set; } = null!;

        #region Behavior
        public static BookingConfig CreateDefault(string tutorId)
        {
            return new BookingConfig
            {
                TutorId = tutorId,
                AllowInstantBooking = true,
                MaxInstantBookingSlots = 3,
            };
        }
        
        public Expression<Func<BookingConfig, object>>[] Update(
            bool allowInstantBooking, 
            int maxInstantBookingSlots)
        {
            var changedProperties = new List<Expression<Func<BookingConfig, object>>>();
            
            if (AllowInstantBooking != allowInstantBooking)
            {
                AllowInstantBooking = allowInstantBooking;
                changedProperties.Add(bc => bc.AllowInstantBooking);
            }
            
            if (MaxInstantBookingSlots != maxInstantBookingSlots)
            {
                MaxInstantBookingSlots = maxInstantBookingSlots;
                changedProperties.Add(bc => bc.MaxInstantBookingSlots);
            }
            
            return changedProperties.ToArray();
        }
        #endregion
    }
}