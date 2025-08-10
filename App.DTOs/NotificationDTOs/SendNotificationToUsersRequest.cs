namespace App.DTOs.NotificationDTOs
{
    public class SendNotificationToUsersRequest
    {
        public NotificationRequest Content { get; set; }
        public List<string> ReceiverUserIds { get; set; }
    }
}
