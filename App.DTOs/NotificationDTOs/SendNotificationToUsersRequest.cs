using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.NotificationDTOs
{
    public class SendNotificationToUsersRequest
    {
        public NotificationRequest Content { get; set; }
        public List<string> ReceiverUserIds { get; set; }
    }
}
