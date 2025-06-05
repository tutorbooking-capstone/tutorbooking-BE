using App.DTOs.ChatDTOs;
using App.Repositories.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Services.Interfaces
{
	public interface IChatService
	{
		Task<ICollection<ChatConversationDTO>> GetConversationsByUserIdAsync(string userId, int page, int size);
		Task<ChatMessageDTO> SendMessageAsync(SendMessageRequest request);
		Task<ChatConversationDTO> GetConversationAsync(string id, int page, int size);

		//Task DeleteChatMessage();
	}
}
