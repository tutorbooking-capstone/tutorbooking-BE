using App.DTOs.ChatDTOs;

namespace App.Services.Interfaces
{
	public interface IChatService
	{
		Task<ICollection<ChatConversationDTO>> GetConversationsByUserIdAsync(int page, int size);
		Task<ChatMessageDTO> SendMessageAsync(SendMessageRequest request);
		Task<ChatConversationDTO> GetConversationAsync(string id, int page, int size);
        Task<ChatMessageDTO> UpdateMessageAsync(UpdateMessageRequest request);
        Task DeleteMessageAsync(string id);
        Task MarkAsReadAsync(string userId, string messageId);

        //Task DeleteChatMessage();
    }
}
