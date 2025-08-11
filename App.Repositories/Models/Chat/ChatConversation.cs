using App.Core.Base;
using App.Repositories.Models.User;

namespace App.Repositories.Models.Chat
{
	public class ChatConversation : BaseEntity
	{
		public virtual ICollection<AppUser> AppUsers { get; set; } 
		public virtual ICollection<ChatMessage> ChatMessages { get; set; }
        public ICollection<ChatConversationReadStatus>? ChatConversationReadStatus { get; set; }
    }
}
