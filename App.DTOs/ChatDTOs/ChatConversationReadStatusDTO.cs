using App.Repositories.Models.Chat;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.ChatDTOs
{
    public class ChatConversationReadStatusDTO
    {
        public string? UserId { get; set; }
        public string? ChatConversationId { get; set; }
        public string? LastReadChatMessageId { get; set; }
        public DateTime? LastReadAt { get; set; }
    }

    public static class ChatConversationReadStatusDTOExtensions
    {
        public static ChatConversationReadStatusDTO ToDTO(this ChatConversationReadStatus entity)
        {
            return new ChatConversationReadStatusDTO()
            {
                UserId = entity.UserId,
                ChatConversationId = entity.ChatConversationId,
                LastReadChatMessageId = entity.LastReadChatMessageId,
                LastReadAt = entity.LastReadAt,
            };
        }
    }
}
