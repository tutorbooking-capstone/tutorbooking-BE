using App.DTOs.ChatDTOs;

namespace TutorBooking.APIService.Hubs.ChatHubs
{
	public interface IChatClient
	{
		Task ReceiveMessage(bool success, ChatMessageDTO message);
		Task ReceiveMessage(bool success, string message);
		Task OnConnected(string message);
		Task OnDisconnected(string message);
	}
}
