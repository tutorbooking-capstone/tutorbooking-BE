using App.Core.Base;
using App.Repositories.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.ChatDTOs
{
	public class CreateConversationResponse : BaseEntity
	{
		public string Id { get; set; }
	}

	public static class CreateDirectConversationResponseExtensions
	{
		public static CreateConversationResponse ToResponseDTO(this ChatConversation conversation)
			=> new CreateConversationResponse()
			{
				Id = conversation.Id,
			};
	}
}
