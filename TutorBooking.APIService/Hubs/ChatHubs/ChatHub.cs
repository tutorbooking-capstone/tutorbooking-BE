using App.Core.Base;
using App.DTOs.ChatDTOs;
using App.Services.Interfaces;
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

        // Thêm các biến static để quản lý rate limiting và concurrent requests
        private static readonly SemaphoreSlim _sendMessageSemaphore = new SemaphoreSlim(10, 10);
        private static readonly SemaphoreSlim _updateMessageSemaphore = new SemaphoreSlim(10, 10);
        private static readonly SemaphoreSlim _deleteMessageSemaphore = new SemaphoreSlim(10, 10);
        private static readonly SemaphoreSlim _markAsReadSemaphore = new SemaphoreSlim(15, 15);
        private static readonly Dictionary<string, DateTime> _lastMessageTime = new Dictionary<string, DateTime>();
        private static readonly Dictionary<string, DateTime> _lastTypingTime = new Dictionary<string, DateTime>();
        private static readonly object _lockObj = new object();
        
        // Thêm counter để theo dõi số lượng connections
        private static int _connectionCount = 0;
        private static readonly object _connectionLock = new object();
        private const int MAX_CONNECTIONS = 200;

        public ChatHub(
            IChatService chatService, 
            ILogger<ChatHub> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            // Giới hạn số lượng connections
            lock (_connectionLock)
            {
                _connectionCount++;
                if (_connectionCount > MAX_CONNECTIONS)
                {
                    _logger.LogWarning("Connection limit reached: {ConnectionCount} connections", _connectionCount);
                    Context.Abort();
                    return;
                }
            }

            var userId = GetUserId();
            _logger.LogInformation("User {UserId} connected to ChatHub", userId);
            await Clients.Caller.OnConnected("CONNECTED_TO_CHATHUB");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            lock (_connectionLock)
            {
                _connectionCount--;
            }

            var userId = GetUserId();
            _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(SendMessageRequest request)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                
                // Rate limiting
                var userId = GetUserId();
                bool canSend = true;
                
                lock (_lockObj)
                {
                    if (_lastMessageTime.TryGetValue(userId, out var lastTime))
                    {
                        if ((DateTime.UtcNow - lastTime).TotalMilliseconds < 500) // 500ms cooldown
                        {
                            canSend = false;
                        }
                    }
                    
                    if (canSend)
                    {
                        _lastMessageTime[userId] = DateTime.UtcNow;
                    }
                }
                
                if (!canSend)
                {
                    await Clients.Caller.SendMessageResult(429, "Rate limit exceeded");
                    return;
                }
                
                // Giới hạn kích thước tin nhắn
                if (request.TextMessage?.Length > 2000)
                {
                    request.TextMessage = request.TextMessage.Substring(0, 2000);
                }
                
                // Semaphore để giới hạn concurrent requests
                if (!await _sendMessageSemaphore.WaitAsync(TimeSpan.FromSeconds(5)))
                {
                    await Clients.Caller.SendMessageResult(503, "Server is busy, please try again later");
                    return;
                }

                try
                {
                    request.SenderUserId = userId;
                    var response = await _chatService.SendMessageAsync(request);

                    await Clients.User(request.ReceiverUserId).ReceiveMessage(response);
                    await Clients.Caller.SendMessageResult(200, response);
                }
                finally
                {
                    _sendMessageSemaphore.Release();
                }
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
                
                // Rate limiting
                var userId = GetUserId();
                bool canProceed = true;
                
                lock (_lockObj)
                {
                    if (_lastMessageTime.TryGetValue(userId, out var lastTime))
                    {
                        if ((DateTime.UtcNow - lastTime).TotalMilliseconds < 1000) // 1s cooldown
                        {
                            canProceed = false;
                        }
                    }
                    
                    if (canProceed)
                    {
                        _lastMessageTime[userId] = DateTime.UtcNow;
                    }
                }
                
                if (!canProceed)
                {
                    await Clients.Caller.UpdateMessageResult(429, "Rate limit exceeded");
                    return;
                }
                
                // Giới hạn kích thước tin nhắn
                if (request.TextMessage?.Length > 2000)
                {
                    request.TextMessage = request.TextMessage.Substring(0, 2000);
                }
                
                // Semaphore để giới hạn concurrent requests
                if (!await _updateMessageSemaphore.WaitAsync(TimeSpan.FromSeconds(5)))
                {
                    await Clients.Caller.UpdateMessageResult(503, "Server is busy, please try again later");
                    return;
                }

                try
                {
                    var response = await _chatService.UpdateMessageAsync(request);
                    await Clients.User(request.ReceiverUserId).OnMessageUpdated(response);
                    await Clients.User(GetUserId()).UpdateMessageResult(200, response);
                }
                finally
                {
                    _updateMessageSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, UPDATE_MESSAGE_RESULT);
            }
        }

        public async Task DeleteMessage(DeleteMessageRequest request)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(request);
                
                // Rate limiting
                var userId = GetUserId();
                bool canProceed = true;
                
                lock (_lockObj)
                {
                    if (_lastMessageTime.TryGetValue(userId, out var lastTime))
                    {
                        if ((DateTime.UtcNow - lastTime).TotalMilliseconds < 1000) // 1s cooldown
                        {
                            canProceed = false;
                        }
                    }
                    
                    if (canProceed)
                    {
                        _lastMessageTime[userId] = DateTime.UtcNow;
                    }
                }
                
                if (!canProceed)
                {
                    await Clients.Caller.DeleteMessageResult(429, "Rate limit exceeded");
                    return;
                }
                
                // Semaphore để giới hạn concurrent requests
                if (!await _deleteMessageSemaphore.WaitAsync(TimeSpan.FromSeconds(5)))
                {
                    await Clients.Caller.DeleteMessageResult(503, "Server is busy, please try again later");
                    return;
                }

                try
                {
                    await _chatService.DeleteMessageAsync(request.Id);
                    await Clients.User(request.ReceiverUserId).OnMessageDeleted(request.Id);
                    await Clients.User(GetUserId()).DeleteMessageResult(200, "SUCCESS");
                }
                finally
                {
                    _deleteMessageSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, DELETE_MESSAGE_RESULT);
            }
        }

        public async Task TypingMessage(string receiverUserId)
        {
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(receiverUserId);
                
                // Rate limiting cho typing events - giảm spam
                var userId = GetUserId();
                bool canSend = true;
                
                lock (_lockObj)
                {
                    if (_lastTypingTime.TryGetValue(userId, out var lastTime))
                    {
                        if ((DateTime.UtcNow - lastTime).TotalMilliseconds < 1000) // 1s cooldown
                        {
                            canSend = false;
                        }
                    }
                    
                    if (canSend)
                    {
                        _lastTypingTime[userId] = DateTime.UtcNow;
                    }
                }
                
                if (canSend)
                {
                    await Clients.User(receiverUserId).OnUserTyping(userId);
                }
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
                
                // Rate limiting
                var userId = GetUserId();
                bool canProceed = true;
                
                lock (_lockObj)
                {
                    if (_lastMessageTime.TryGetValue(userId, out var lastTime))
                    {
                        if ((DateTime.UtcNow - lastTime).TotalMilliseconds < 300) // 300ms cooldown
                        {
                            canProceed = false;
                        }
                    }
                    
                    if (canProceed)
                    {
                        _lastMessageTime[userId] = DateTime.UtcNow;
                    }
                }
                
                if (!canProceed)
                {
                    await Clients.Caller.MarkAsReadResult(429, "Rate limit exceeded");
                    return;
                }
                
                // Semaphore để giới hạn concurrent requests
                if (!await _markAsReadSemaphore.WaitAsync(TimeSpan.FromSeconds(3)))
                {
                    await Clients.Caller.MarkAsReadResult(503, "Server is busy, please try again later");
                    return;
                }

                try
                {
                    await _chatService.MarkAsReadAsync(GetUserId(), messageId);
                    await Clients.User(GetUserId()).MarkAsReadResult(200, "SUCCESS");
                    await Clients.User(receiverUserId).OnMessageRead(messageId);
                }
                finally
                {
                    _markAsReadSemaphore.Release();
                }
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
