using System.ComponentModel.DataAnnotations;

namespace App.DTOs.ChatDTOs
{
	public class CreateConversationRequest
	{
		[Length(2,2)]
		public ICollection<string>? ParticipantUserIds { get; set; }
	}
}
