using App.Repositories.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.ChatDTOs
{
	public class SendMessageResponse
	{
		public string Id { get; set; }
		public string UserId { get; set; }
		public string ChatConversationId { get; set; }
		public string TextMessage { get; set; }
	}

	public static class SendMessageResponseExtensions
	{
		public static SendMessageResponse ToMessageResponse(this ChatMessage message)
			=> new SendMessageResponse()
			{
				Id = message.Id,
				UserId = message.AppUserId,
				ChatConversationId = message.ChatConversationId,
				TextMessage = message.TextMessage
			};
	}
}
