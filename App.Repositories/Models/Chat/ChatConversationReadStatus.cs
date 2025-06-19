using App.Core.Base;
using App.Repositories.Models.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Repositories.Models.Chat
{
    public class ChatConversationReadStatus : BaseEntity
    {
        public string UserId { get; set; }
        public string ChatConversationId { get; set; }
        public string LastReadChatMessageId { get; set; }

        public virtual AppUser AppUser { get; set; }
        public virtual ChatConversation ChatConversation { get; set; }
        public virtual ChatMessage LastReadChatMessage { get; set; }
    }
}
