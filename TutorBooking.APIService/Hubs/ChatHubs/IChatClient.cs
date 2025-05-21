using App.DTOs.ChatDTOs;

namespace TutorBooking.APIService.Hubs.ChatHubs
{
	public interface IChatClient
	{
		Task ReceiveMessage(int status, ChatMessageDTO message);
		Task ReceiveMessage(int status, string error);
		Task OnConnected(string message);
		Task OnDisconnected(string message);
	}
}
