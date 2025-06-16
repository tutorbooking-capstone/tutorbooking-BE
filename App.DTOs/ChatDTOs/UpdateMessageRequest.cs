using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.ChatDTOs
{
    public class UpdateMessageRequest
    {
        public string Id { get; set; }
        public string ReceiverUserId { get; set; }
        public string TextMessage { get; set; }
    }
}
