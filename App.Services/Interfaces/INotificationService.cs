using App.DTOs.NotificationDTOs;

namespace App.Services.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationResponse> CreateForRolesAsync(SendNotificationToRolesRequest request);
        Task<NotificationResponse> CreateForUsersAsync(SendNotificationToUsersRequest request);
        Task<List<NotificationResponse>> GetNotificationsOfUserAsync(int page, int size, bool isUnreadOnly);
        Task MarkAsReadAsync(string notificationId, string userId);
    }
}