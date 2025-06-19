using App.Core.Base;
using App.DTOs.ChatDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Security;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;

namespace TutorBooking.APIService.Hubs.ChatHubs
{
    [Authorize]
//#pragma warning disable CS4014
    public class ChatHub : Hub<IChatClient>
    {
        private IChatService _chatService;
        private ILogger<ChatHub> _logger;

        public ChatHub(IChatService chatService, ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId != null)
            {
                _logger.LogInformation($"{userId} Connected");
                ConnectionMapper.Set(userId, Context.ConnectionId);
                await Clients.Client(Context.ConnectionId).OnConnected("CONNECTED_TO_CHATHUB");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId != null)
            {
                var user = ConnectionMapper.Get(userId);
                if (user != null)
                    ConnectionMapper.RemoveConnectedUser(userId);
                _logger.LogInformation($"{userId} Disconnected");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(SendMessageRequest request)
        {
            try
            {
                request.SenderUserId = GetUserId();
                var response = await _chatService.SendMessageAsync(request);

                var user = ConnectionMapper.Get(request.ReceiverUserId);
                if (user != null) await Clients.Client(user).ReceiveMessage(response);
                user = ConnectionMapper.Get(GetUserId());
                if (user != null) await Clients.Client(user).SendMessageResult(200, response);
            }
            catch (ErrorException ex)
            {
                _logger.LogError(ex.ToString());
                var user = ConnectionMapper.Get(GetUserId());
                if (user != null) await Clients.Client(user).SendMessageResult(ex.StatusCode, ex.ErrorDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                var user = ConnectionMapper.Get(GetUserId());
                if (user != null) await Clients.Client(user).SendMessageResult(500, ex.Message);
            }
        }

        public async Task UpdateMessage(UpdateMessageRequest request)
        {
            try
            {
                var response = await _chatService.UpdateMessageAsync(request);

                var user = ConnectionMapper.Get(request.ReceiverUserId);
                if (user != null) await Clients.Client(user).OnMessageUpdated(response);
                user = ConnectionMapper.Get(GetUserId());
                if (user != null) await Clients.Client(user).UpdateMessageResult(200, response);              
            }
            catch (ErrorException ex)
            {
                _logger.LogError(ex.ToString());
                var user = ConnectionMapper.Get(GetUserId());
                if (user != null) await Clients.Client(user).SendMessageResult(ex.StatusCode, ex.ErrorDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                var user = ConnectionMapper.Get(GetUserId());
                if (user != null) await Clients.Client(user).UpdateMessageResult(500, ex.Message);
            }
        }

        /// <summary>
        /// Deletes a message, then notifies the specified user that the message has been deleted.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task DeleteMessage(DeleteMessageRequest request)
        {
            try
            {
                await _chatService.DeleteMessageAsync(request.Id);

                var user = ConnectionMapper.Get(request.ReceiverUserId);
                if (user != null) await Clients.Client(user).OnMessageDeleted(request.Id);
                user = ConnectionMapper.Get(GetUserId());
                if (user != null) await Clients.Client(user).DeleteMessageResult(200, "SUCCESS");

            }
            catch (ErrorException ex)
            {
                _logger.LogError(ex.ToString());
                var user = ConnectionMapper.Get(GetUserId());
                if (user != null) await Clients.Client(user).SendMessageResult(ex.StatusCode, ex.ErrorDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                var user = ConnectionMapper.Get(GetUserId());
                if (user != null) await Clients.Client(user).DeleteMessageResult(500, ex.Message);
            }
        }

        /// <summary>
        /// Sends a message to the specified user notifying that the connected user is typing
        /// </summary>
        /// <param name="receiverUserId"></param>
        /// <returns></returns>
        public async Task TypingMessage(string receiverUserId)
        {
            try
            {
                var receiver = ConnectionMapper.Get(receiverUserId);
                if (receiver != null) await Clients.Client(receiver).OnUserTyping(GetUserId());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
        }

        public async Task MarkAsRead(string messageId, string receiverUserId)
        {
            try
            {
                await _chatService.MarkAsReadAsync(GetUserId(), messageId);
                var user = ConnectionMapper.Get(GetUserId());
                if (user != null) await Clients.Client(user).MarkAsReadResult(200, "SUCCESS");
                user = ConnectionMapper.Get(receiverUserId);
                if (user != null) await Clients.Client(user).OnMessageRead(user);
            }
            catch (ErrorException ex)
            {
                _logger.LogError(ex.ToString());
                var user = ConnectionMapper.Get(GetUserId());
                if (user != null) await Clients.Client(user).MarkAsReadResult(ex.StatusCode, ex.ErrorDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                var user = ConnectionMapper.Get(GetUserId());
                if (user != null) await Clients.Client(user).MarkAsReadResult(500, ex.Message);
            }
        }

        /// <summary>
        /// Gets the UserId of the connected user
        /// </summary>
        /// <returns></returns>
        private string GetUserId()
        {
            try
            {
                //var token = Context.GetHttpContext().Request.Query.FirstOrDefault(c => c.Key.Equals("access_token")).Value;
                var token2 = Context.GetHttpContext().Request.Headers[HeaderNames.Authorization].ToString().Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var securityToken = handler.ReadJwtToken(token2);
                var userId = securityToken.Claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.Sub)).Value;
                return userId;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
