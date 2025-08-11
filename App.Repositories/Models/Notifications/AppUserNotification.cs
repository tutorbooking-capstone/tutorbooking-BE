using App.Repositories.Models.User;

namespace App.Repositories.Models.Notifications
{
    public class AppUserNotification
    {
        public string AppUserId { get; set; } = null!;
        public string NotificationEntityId { get; set; } = null!;
        public DateTime? ReadAt { get; set; }
        public virtual AppUser? AppUser { get; set; }
        public virtual NotificationEntity? NotificationEntity { get; set; }

    }
}
