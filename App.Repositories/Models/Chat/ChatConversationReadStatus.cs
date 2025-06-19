using App.Core.Base;
using App.Repositories.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Repositories.Models.Chat
{
    public class ChatConversationReadStatus : CoreEntity
    {
        public string UserId { get; set; } = null!;
        public string ChatConversationId { get; set; } = null!;
        public string LastReadChatMessageId { get; set; } = null!;
        public DateTime LastReadAt { get; set; } = DateTime.UtcNow;

        public virtual AppUser AppUser { get; set; }
        public virtual ChatConversation ChatConversation { get; set; }
        public virtual ChatMessage LastReadChatMessage { get; set; }
    }
}
