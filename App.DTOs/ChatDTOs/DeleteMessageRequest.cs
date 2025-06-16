using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.ChatDTOs
{
    public class DeleteMessageRequest
    {
        public string Id { get; set; }
        public string ReceiverUserId { get; set; }
    }
}
