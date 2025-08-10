using App.Repositories.Models.User;

namespace App.DTOs.NotificationDTOs
{
    public class SendNotificationToRolesRequest
    {
        public NotificationRequest Content { get; set; }
        public List<Role> Roles { get; set; }
    }
}
