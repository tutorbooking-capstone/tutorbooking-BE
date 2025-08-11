using App.Repositories.Models.Notifications;

namespace App.DTOs.NotificationDTOs
{
    public class NotificationResponse
    {
        public string Id { get; set; }
        public ENotificationPriority NotificationPriority { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string? AdditionalData { get; set; }
        public bool? isRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public static class NotificationResposneExtensions
    {
        public static NotificationResponse ToResponse(this NotificationEntity entity)
            => new NotificationResponse
            {
                Id = entity.Id,
                NotificationPriority = entity.NotificationPriority,
                Title = entity.Title,
                Content = entity.Content,
                AdditionalData = entity.AdditionalData,
                CreatedAt = entity.CreatedAt,
            };
    }
}
