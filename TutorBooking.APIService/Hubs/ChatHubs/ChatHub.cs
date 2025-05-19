using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Org.BouncyCastle.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;

namespace TutorBooking.APIService.Hubs.ChatHubs
{
	
	public class ChatHub : Hub<IChatClient>, IChatHub
	{
		private IChatService _chatService;

		public ChatHub(IChatService chatService)
		{
			_chatService = chatService;
		}

		public async Task SendMessage(string senderUserId, string receiverUserId, string textMessage)
		{
			try
			{
				var response = await _chatService.SendMessageAsync(new()
				{
					SenderUserId = senderUserId,
					ReceiverUserId = receiverUserId,
					TextMessage = textMessage
				});

				var receiver = ConnectionMapper.GetConnectedUser(receiverUserId);
				if (receiver != null)
					await Clients.Client(receiver.ConnectionId).ReceiveMessage(true, response);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				await Clients.Client(ConnectionMapper.GetConnectedUser(senderUserId).ConnectionId).ReceiveMessage(false, ex.Message);
			}
		}

		public override async Task OnConnectedAsync()
		{
			var token = Context.GetHttpContext().Request.Query.FirstOrDefault(c => c.Key.Equals("jwt")).Value;
			var handler = new JwtSecurityTokenHandler();
			var securityToken = handler.ReadJwtToken(token);

			//var identity = Context.User.Claims as ClaimsIdentity;
			//var userId = identity.Claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.Sub)).Value;
			var userId = securityToken.Claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.Sub)).Value;
			Console.WriteLine($"Client {userId} has connected");
			var user = new ConnectedUser()
			{
					UserId = userId,
					ConnectionId = Context.ConnectionId,
					//Role = identity.RoleClaimType.ToRoleEnum()
				};
				ConnectionMapper.SetConnectedUser(userId, user);
				await Clients.Client(Context.ConnectionId).OnConnected("CONNECTED_TO_CHATHUB");
			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{

			//var identity = Context.User.Claims as ClaimsIdentity;
			//var userId = identity.Claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.Sub)).Value;
			var token = Context.GetHttpContext().Request.Query.FirstOrDefault(c => c.Key.Equals("jwt")).Value;
			var handler = new JwtSecurityTokenHandler();
			var securityToken = handler.ReadJwtToken(token);
			var userId = securityToken.Claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.Sub)).Value;

			var user = ConnectionMapper.GetConnectedUser(userId);
			if (user != null)
			{
				ConnectionMapper.RemoveConnectedUser(userId);
			}
			await base.OnDisconnectedAsync(exception);
		}
	}


}
