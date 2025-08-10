using System.ComponentModel.DataAnnotations;

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
