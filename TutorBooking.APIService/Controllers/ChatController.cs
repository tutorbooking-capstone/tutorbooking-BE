using App.Core.Base;
using App.Core.Utils;
using App.DTOs.ChatDTOs;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Org.BouncyCastle.Security;
using System.Security.Claims;
using TutorBooking.APIService.Hubs;
using TutorBooking.APIService.Hubs.ChatHubs;

namespace TutorBooking.APIService.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ChatController : ControllerBase
	{
		private IHubContext<ChatHub, IChatClient> _hubContext;
		private IChatService _chatService;

		public ChatController(IChatService chatService, IHubContext<ChatHub, IChatClient> hubContext)
		{
			_chatService = chatService;
			_hubContext = hubContext;
		}

		[HttpGet("conversations")]
		[Authorize]
		public async Task<IActionResult> GetConversations([FromQuery]string userId, int page = 1, int size = 20)
		{
			return Ok(new BaseResponseModel<object>(
				data: await _chatService.GetConversationsByUserIdAsync(userId, page, size)
				));
		}

		[HttpGet("conversation/{id}")]
		[Authorize]
		public async Task<IActionResult> GetConversationById([FromRoute]string id, [FromQuery]int page = 1 , int size = 20)
		{
			return Ok(new BaseResponseModel<object>(
				data: await _chatService.GetConversationAsync(id, page, size)
				));
		}
    }
}
