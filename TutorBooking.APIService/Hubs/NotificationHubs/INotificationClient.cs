namespace TutorBooking.APIService.Hubs.NotificationHubs
{
	public interface INotificationClient
	{
		Task ReceiveNotification(string message);

		Task UserConnected(string username);
		Task UserDisconnected(string username);
	}
}
