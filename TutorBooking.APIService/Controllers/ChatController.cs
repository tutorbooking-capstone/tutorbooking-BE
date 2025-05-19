using App.Core.Base;
using App.DTOs.ChatDTOs;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Org.BouncyCastle.Security;
using TutorBooking.APIService.Hubs;
using TutorBooking.APIService.Hubs.ChatHubs;

namespace TutorBooking.APIService.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ChatController : ControllerBase
	{
		private IHubContext<ChatHub> _hubContext;
		private IChatService _chatService;

		public ChatController(IChatService chatService, IHubContext<ChatHub> hubContext)
		{
			_chatService = chatService;
			_hubContext = hubContext;
		}

		[HttpGet("conversations")]
		//[Authorize]
		[AllowAnonymous] // for quick testing only
		public async Task<IActionResult> GetConversations([FromQuery] string userId, int page = 1, int size = 20)
		{
			return Ok(new BaseResponseModel<object>(
				data: await _chatService.GetConversationsByUserIdAsync(userId, page, size)
				));
		}

		[HttpGet("conversations/{id}")]
		[AllowAnonymous]
		public async Task<IActionResult> GetConversationById([FromRoute]string id, [FromQuery]int page =1 , int size = 20)
		{
			return Ok(new BaseResponseModel<object>(
				data: await _chatService.GetConversationAsync(id, page, size)
				));
		}

		[HttpPost("message")]
		//[Authorize]
		[AllowAnonymous]
		public async Task<IActionResult> SendMessage(SendMessageRequest request)
		{
			var response = await _chatService.SendMessageAsync(request);
			_hubContext.Clients.Client(ConnectionMapper.GetConnectedUser(request.ReceiverUserId).ConnectionId)
				.SendAsync("ReceiveMessage", response);
			return Ok(new BaseResponseModel<object>()
			{
				Data = response
			});
		}
	}
}
