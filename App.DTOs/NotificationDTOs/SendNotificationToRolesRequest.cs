using App.Repositories.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.NotificationDTOs
{
    public class SendNotificationToRolesRequest
    {
        public NotificationRequest Content { get; set; }
        public List<Role> Roles { get; set; }
    }
}
