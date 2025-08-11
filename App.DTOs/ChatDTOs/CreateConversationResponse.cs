using App.Core.Base;
using App.Repositories.Models.Chat;

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
