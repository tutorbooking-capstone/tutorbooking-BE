using App.DTOs.NotificationDTOs;

namespace App.Services.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationResponse> SendToRolesAsync(SendNotificationToRolesRequest request);
        Task<NotificationResponse> SendToUsersAsync(SendNotificationToUsersRequest request);
        Task<List<NotificationResponse>> GetNotificationsOfUserAsync(int page, int size, bool isUnreadOnly);
        Task<NotificationSenderResponse> GetSenderByIdAsync(string id);
        Task<NotificationSenderResponse> GetTutorSenderByIdAsync(string id);
        Task MarkAsReadAsync(string notificationId, string userId);
        Task MarkAllAsReadAsync(string userId);
    }
}