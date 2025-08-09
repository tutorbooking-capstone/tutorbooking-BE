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
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IChatService chatService, 
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} connected to ChatHub", userId);
            await Clients.Caller.OnConnected("CONNECTED_TO_CHATHUB");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(SendMessageRequest request)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                
                request.SenderUserId = GetUserId();
                var response = await _chatService.SendMessageAsync(request);

                await Clients.User(request.ReceiverUserId).ReceiveMessage(response);
                await Clients.Caller.SendMessageResult(200, response);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, SEND_MESSAGE_RESULT);
            }
        }

        public async Task UpdateMessage(UpdateMessageRequest request)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                
                var response = await _chatService.UpdateMessageAsync(request);

                await Clients.User(request.ReceiverUserId).OnMessageUpdated(response);
                await Clients.User(GetUserId()).UpdateMessageResult(200, response);              
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, UPDATE_MESSAGE_RESULT);
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
                ArgumentNullException.ThrowIfNull(request);
                
                await _chatService.DeleteMessageAsync(request.Id);

                await Clients.User(request.ReceiverUserId).OnMessageDeleted(request.Id);
                await Clients.User(GetUserId()).DeleteMessageResult(200, "SUCCESS");
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, DELETE_MESSAGE_RESULT);
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
                ArgumentException.ThrowIfNullOrWhiteSpace(receiverUserId);
                
                await Clients.User(receiverUserId).OnUserTyping(GetUserId());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TypingMessage for receiver {ReceiverId}", receiverUserId);
            }
        }

        public async Task MarkAsRead(string messageId, string receiverUserId)
        {
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
                ArgumentException.ThrowIfNullOrWhiteSpace(receiverUserId);
                
                await _chatService.MarkAsReadAsync(GetUserId(), messageId);
                
                await Clients.User(GetUserId()).MarkAsReadResult(200, "SUCCESS");
                await Clients.User(receiverUserId).OnMessageRead(messageId);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, MARK_AS_READ_RESULT);
            }
        }

        /// <summary>
        /// Gets the UserId of the connected user
        /// </summary>
        /// <returns></returns>
        private string GetUserId() => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User ID not found");


        private const string SEND_MESSAGE_RESULT = nameof(IChatClient.SendMessageResult);
        private const string UPDATE_MESSAGE_RESULT = nameof(IChatClient.UpdateMessageResult);
        private const string DELETE_MESSAGE_RESULT = nameof(IChatClient.DeleteMessageResult);
        private const string MARK_AS_READ_RESULT = nameof(IChatClient.MarkAsReadResult);
        private async Task HandleExceptionAsync(Exception ex, string resultMethod)
        {
            var (statusCode, errorMessage) = ex switch
            {
                ErrorException errorEx => (errorEx.StatusCode, (object)errorEx.ErrorDetail),
                _ => (500, (object)ex.Message)
            };

            if (ex is ErrorException)
                _logger.LogWarning("Business error in ChatHub: {Message}", ex.Message);
            else
                _logger.LogError(ex, "Exception in ChatHub: {Message}", ex.Message);

            Func<Task> clientMethod = resultMethod switch
            {
                SEND_MESSAGE_RESULT => () => Clients.Caller.SendMessageResult(statusCode, errorMessage),
                UPDATE_MESSAGE_RESULT => () => Clients.Caller.UpdateMessageResult(statusCode, errorMessage),
                DELETE_MESSAGE_RESULT => () => Clients.Caller.DeleteMessageResult(statusCode, errorMessage),
                MARK_AS_READ_RESULT => () => Clients.Caller.MarkAsReadResult(statusCode, errorMessage),
                _ => throw new ArgumentException($"Unknown result method: {resultMethod}")
            };

            await clientMethod();
        }
    }
}
