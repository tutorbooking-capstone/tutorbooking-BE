using App.Repositories.Models.Scheduling;

namespace App.Repositories.Models.User
{
    public class Learner
    {
        public string UserId { get; set; } = string.Empty;

        public virtual AppUser? User { get; set; }
        public virtual ICollection<BookingSlot>? BookingSlots { get; set; }
    }
}
