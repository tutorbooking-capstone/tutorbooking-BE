using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TutorBooking.APIService.Hubs.NotificationHubs
{
	[AllowAnonymous]
	public class NotificationHub : Hub<INotificationClient>
	{
		public async Task SendNotificationAsync(string message)
		{
			await Clients.All.ReceiveNotification(message);
		}

		public async Task TestNotificationAsync()
		{
			await Clients.All.ReceiveNotification("This is a test");
		}

		public override async Task OnConnectedAsync()
		{
			await base.OnConnectedAsync();
			await Clients.All.UserConnected(Context.ConnectionId);
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			await base.OnDisconnectedAsync(exception);
			await Clients.All.UserDisconnected(Context.ConnectionId);
		}
	}
}
