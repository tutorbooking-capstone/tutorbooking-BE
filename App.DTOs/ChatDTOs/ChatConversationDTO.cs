using App.Repositories.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.ChatDTOs
{
	public class ChatConversationDTO
	{
		public string Id { get; set; } = string.Empty;
		public ICollection<ChatMessageDTO> Messages { get; set; } = new List<ChatMessageDTO>();
		public ICollection<ChatParticipantDTO> Participants { get; set; } = new List<ChatParticipantDTO>();
	}

	public static class ChatConversationDTOExtenstions
	{
		public static ChatConversationDTO ToChatConversationDTO(this ChatConversation entity)
		{
			var response = new ChatConversationDTO();
			response.Id = entity.Id;
			foreach (var message in entity.ChatMessages)
			{
				response.Messages.Add(message.ToChatMessageDTO());
			}

			foreach (var appUser in entity.AppUsers)
			{
				response.Participants.Add(appUser.ToChatParticipantDTO());
			}

			return response;
		}
	}
}
