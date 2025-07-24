using App.Repositories.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
