using App.DTOs.ChatDTOs;

namespace TutorBooking.APIService.Hubs.ChatHubs
{
	public interface IChatClient
	{
        Task OnConnected(string message);
        Task OnDisconnected(string message);

        Task ReceiveMessage(ChatMessageDTO message);
		Task OnMessageUpdated (ChatMessageDTO message);
		Task OnMessageDeleted (string messageId);
		Task OnMessageRead(string messageId);
		Task OnUserTyping(string userId);

		Task SendMessageResult(int status, object data);
		Task UpdateMessageResult(int status, object data);
		Task DeleteMessageResult(int status, object data);
		Task MarkAsReadResult(int status, object data);
	}
}
