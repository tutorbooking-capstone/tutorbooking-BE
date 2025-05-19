namespace TutorBooking.APIService.Hubs.ChatHubs
{
	public interface IChatHub
	{
		Task OnConnectedAsync();
		Task OnDisconnectedAsync(Exception? exception);
		Task SendMessage(string senderUserId, string receiverUserId, string message);
	}
}