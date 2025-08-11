using App.Core.Base;
using App.Repositories.Models.User;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace App.Repositories.Models.Chat
{
	public class ChatMessage : BaseEntity
	{
		[Required]
		public string AppUserId { get; set; } = null!;
		[Required]
		public string ChatConversationId { get; set; } = null!;
		public string? TextMessage { get; set; }
		

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public virtual AppUser AppUser { get; set; } = null!;

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
		public virtual ChatConversation ChatConversation { get; set; } = null!;
		public virtual ICollection<FileUpload>? FileUploads { get; set; }

		public virtual ICollection<ChatConversationReadStatus> ChatConversationReadStatuses { get; set;}

    }

	
}
