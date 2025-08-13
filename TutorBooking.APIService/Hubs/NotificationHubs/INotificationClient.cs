using App.DTOs.NotificationDTOs;

namespace TutorBooking.APIService.Hubs.NotificationHubs
{
	public interface INotificationClient
	{
		Task ReceiveNotification(int statusCode, NotificationResponse notification);
		Task MarkAsReadResult(int statusCode, object data);
		Task MarkAllAsReadResult(int statusCode, object data);
        Task UserConnected(string message);
		Task UserDisconnected(string message);
	}
}
