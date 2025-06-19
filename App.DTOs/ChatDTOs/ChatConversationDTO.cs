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
		public ICollection<ChatConversationReadStatusDTO> ChatConversationReadStatus { get; set; } = new List<ChatConversationReadStatusDTO>();
    }

	public static class ChatConversationDTOExtenstions
	{
		public static async Task<ChatConversationDTO> ToChatConversationDTO(this ChatConversation entity)
		{
			var response = new ChatConversationDTO();
			response.Id = entity.Id;
			var task1 = Task.Run(() =>
			{
				for (var i = entity.ChatMessages.Count -1; i >= 0; i--)
				{
					response.Messages.Add(entity.ChatMessages.ElementAt(i).ToChatMessageDTO());
				}
			});

			var task2 = Task.Run(() => {
				foreach (var appUser in entity.AppUsers)
				{
					response.Participants.Add(appUser.ToChatParticipantDTO());
				}
			});

			var task3 = Task.Run(() =>
			{
				if (entity.ChatConversationReadStatus != null)
					foreach (var readStatus in entity.ChatConversationReadStatus)
						response.ChatConversationReadStatus.Add(readStatus.ToDTO());
			});
			await Task.WhenAll(task1, task2, task3);
			return response;
		}
	}
}
