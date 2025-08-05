using App.Core.Base;
using App.DTOs.ChatDTOs;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace TutorBooking.APIService.Hubs.ChatHubs
{
    [Authorize]
    public class ChatHub : Hub<IChatClient>
    {
        private readonly IChatService _chatService;
        private readonly IUserService _userService;
        private readonly ILogger<ChatHub> _logger;
        private readonly ConnectionService _connectionService;

        public ChatHub(
            IChatService chatService, 
            ILogger<ChatHub> logger, 
            IUserService userService,
            ConnectionService connectionService)
        {
            _chatService = chatService;
            _logger = logger;
            _userService = userService;
            _connectionService = connectionService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation($"{userId} connected to ChatHub");
                _connectionService.AddConnection(userId, Context.ConnectionId);
                await Clients.Caller.OnConnected("CONNECTED_TO_CHATHUB");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                _connectionService.RemoveConnection(userId);
                _logger.LogInformation($"{userId} disconnected from ChatHub");
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(SendMessageRequest request)
        {
            try
            {
                request.SenderUserId = GetUserId();
                var response = await _chatService.SendMessageAsync(request);

                // Gửi tin nhắn cho người nhận nếu họ đang online
                var receiverConnectionId = _connectionService.GetConnectionId(request.ReceiverUserId);
                if (!string.IsNullOrEmpty(receiverConnectionId))
                    await Clients.Client(receiverConnectionId).ReceiveMessage(response);
                
                // Gửi kết quả về cho người gửi
                await Clients.Caller.SendMessageResult(200, response);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, "SendMessageResult");
            }
        }

        public async Task UpdateMessage(UpdateMessageRequest request)
        {
            try
            {
                var response = await _chatService.UpdateMessageAsync(request);

                var receiverConnectionId = _connectionService.GetConnectionId(request.ReceiverUserId);
                if (!string.IsNullOrEmpty(receiverConnectionId)) await Clients.Client(receiverConnectionId).OnMessageUpdated(response);
                var senderConnectionId = _connectionService.GetConnectionId(GetUserId());
                if (!string.IsNullOrEmpty(senderConnectionId)) await Clients.Client(senderConnectionId).UpdateMessageResult(200, response);              
            }
            catch (ErrorException ex)
            {
                _logger.LogError(ex.ToString());
                var user = _connectionService.GetConnectionId(GetUserId());
                if (!string.IsNullOrEmpty(user)) await Clients.Client(user).SendMessageResult(ex.StatusCode, ex.ErrorDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                var user = _connectionService.GetConnectionId(GetUserId());
                if (!string.IsNullOrEmpty(user)) await Clients.Client(user).UpdateMessageResult(500, ex.Message);
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

                var user = _connectionService.GetConnectionId(request.ReceiverUserId);
                if (!string.IsNullOrEmpty(user)) await Clients.Client(user).OnMessageDeleted(request.Id);
                user = _connectionService.GetConnectionId(GetUserId());
                if (!string.IsNullOrEmpty(user)) await Clients.Client(user).DeleteMessageResult(200, "SUCCESS");

            }
            catch (ErrorException ex)
            {
                _logger.LogError(ex.ToString());
                var user = _connectionService.GetConnectionId(GetUserId());
                if (!string.IsNullOrEmpty(user)) await Clients.Client(user).SendMessageResult(ex.StatusCode, ex.ErrorDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                var user = _connectionService.GetConnectionId(GetUserId());
                if (!string.IsNullOrEmpty(user)) await Clients.Client(user).DeleteMessageResult(500, ex.Message);
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
                var receiver = _connectionService.GetConnectionId(receiverUserId);
                if (!string.IsNullOrEmpty(receiver)) await Clients.Client(receiver).OnUserTyping(GetUserId());
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
                
                var senderConnectionId = _connectionService.GetConnectionId(GetUserId());
                if (!string.IsNullOrEmpty(senderConnectionId)) 
                    await Clients.Client(senderConnectionId).MarkAsReadResult(200, "SUCCESS");
                    
                var receiverConnectionId = _connectionService.GetConnectionId(receiverUserId);
                if (!string.IsNullOrEmpty(receiverConnectionId)) 
                    await Clients.Client(receiverConnectionId).OnMessageRead(messageId);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, "MarkAsReadResult");
            }
        }

        /// <summary>
        /// Gets the UserId of the connected user
        /// </summary>
        /// <returns></returns>
        private string GetUserId() => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User ID not found");

        private async Task HandleExceptionAsync(Exception ex, string resultMethod)
        {
            int statusCode = 500;
            object errorMessage = ex.Message;

            if (ex is ErrorException errorEx)
            {
                statusCode = errorEx.StatusCode;
                errorMessage = errorEx.ErrorDetail;
                _logger.LogWarning($"Business error in ChatHub: {errorEx.Message}");
            }
            else
            {
                _logger.LogError(ex, $"Exception in ChatHub: {ex.Message}");
            }

            // Gọi phương thức kết quả tương ứng trên client
            switch (resultMethod)
            {
                case "SendMessageResult":
                    await Clients.Caller.SendMessageResult(statusCode, errorMessage);
                    break;
                case "UpdateMessageResult":
                    await Clients.Caller.UpdateMessageResult(statusCode, errorMessage);
                    break;
                case "DeleteMessageResult":
                    await Clients.Caller.DeleteMessageResult(statusCode, errorMessage);
                    break;
                case "MarkAsReadResult":
                    await Clients.Caller.MarkAsReadResult(statusCode, errorMessage);
                    break;
            }
        }
    }
}
