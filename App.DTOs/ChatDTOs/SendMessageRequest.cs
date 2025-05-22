using App.Repositories.Models.Chat;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.DTOs.ChatDTOs
{
	public class SendMessageRequest
	{
		[Required]
		public string SenderUserId { get; set; }
		[Required]
		public string ReceiverUserId { get; set; }
		public string TextMessage { get; set; }
	}

	public static class SendMessageRequestExtensions
	{

	}
}
