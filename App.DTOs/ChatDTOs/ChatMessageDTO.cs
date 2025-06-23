using App.Repositories.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.ChatDTOs
{
	public class ChatMessageDTO
	{
		public string Id { get; set; }
		public string UserId { get; set; }
		public string ChatConversationId { get; set; }
		public string? TextMessage { get; set; }
		public DateTimeOffset CreatedTime { get; set; }
	}

	public static class ChatMesasgeResponseExtensions
	{
		public static ChatMessageDTO ToChatMessageDTO(this ChatMessage entity)
			=> new()
			{
				Id = entity.Id,
				UserId = entity.AppUserId,
				ChatConversationId = entity.ChatConversationId,
				TextMessage = entity.DeletedTime == null? entity.TextMessage : null,
				CreatedTime = entity.CreatedTime,
			}; 

		public static List<ChatMessageDTO> ToResponseDTOs(this ICollection<ChatMessage> entities)
		{
			var list = new List<ChatMessageDTO>();
			foreach (var item in entities)
			{
				list.Add(ToChatMessageDTO(item));
			}
			return list;
		}
	}
}
