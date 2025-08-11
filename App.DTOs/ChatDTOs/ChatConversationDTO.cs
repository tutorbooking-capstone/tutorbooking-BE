using App.Repositories.Models.Chat;

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
		public static ChatConversationDTO ToChatConversationDTO(this ChatConversation entity)
		{
			var response = new ChatConversationDTO
			{
				Id = entity.Id,
				Messages = entity.ChatMessages
					.OrderByDescending(m => m.CreatedTime)
					.Select(m => m.ToChatMessageDTO())
					.ToList(),
				Participants = entity.AppUsers
					.Select(u => u.ToChatParticipantDTO())
					.ToList(),
				ChatConversationReadStatus = entity.ChatConversationReadStatus?
					.Select(r => r.ToDTO())
					.ToList() ?? new List<ChatConversationReadStatusDTO>()
			};

			return response;
		}
	}
}
