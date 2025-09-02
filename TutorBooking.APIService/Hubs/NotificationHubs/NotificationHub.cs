using App.Core.Base;
using App.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace TutorBooking.APIService.Hubs.NotificationHubs
{
    [Authorize]
    public class NotificationHub : Hub<INotificationClient>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationHub> _logger;

        // Thêm các biến static để quản lý rate limiting và concurrent requests
        private static readonly SemaphoreSlim _markAsReadSemaphore = new SemaphoreSlim(15, 15);
        private static readonly SemaphoreSlim _markAllAsReadSemaphore = new SemaphoreSlim(5, 5);
        private static readonly Dictionary<string, DateTime> _lastOperationTime = new Dictionary<string, DateTime>();
        private static readonly object _lockObj = new object();
        
        // Thêm counter để theo dõi số lượng connections
        private static int _connectionCount = 0;
        private static readonly object _connectionLock = new object();
        private const int MAX_CONNECTIONS = 300; // Notification hub có thể có nhiều connections hơn

        public NotificationHub(
            INotificationService notificationService, 
            ILogger<NotificationHub> logger)
        {
            _notificationService = notificationService;
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
            _logger.LogInformation("User {UserId} connected to NotificationHub", userId);

            var roles = GetUserRolesFromClaims();

            foreach (var role in roles)
                await Groups.AddToGroupAsync(Context.ConnectionId, role);
                
            await Clients.Caller.UserConnected("CONNECTED_TO_NOTIFICATION_HUB");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            lock (_connectionLock)
            {
                _connectionCount--;
            }

            var userId = GetUserId();
            _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task MarkAsRead(string notificationId)
        {
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(notificationId);

                // Rate limiting
                var userId = GetUserId();
                bool canProceed = true;
                
                lock (_lockObj)
                {
                    if (_lastOperationTime.TryGetValue(userId, out var lastTime))
                    {
                        if ((DateTime.UtcNow - lastTime).TotalMilliseconds < 300) // 300ms cooldown
                        {
                            canProceed = false;
                        }
                    }
                    
                    if (canProceed)
                    {
                        _lastOperationTime[userId] = DateTime.UtcNow;
                    }
                }
                
                if (!canProceed)
                {
                    await Clients.Caller.MarkAsReadResult(429, "Rate limit exceeded");
                    return;
                }

                // Semaphore để giới hạn concurrent requests với timeout
                if (!await _markAsReadSemaphore.WaitAsync(TimeSpan.FromSeconds(3)))
                {
                    await Clients.Caller.MarkAsReadResult(503, "Server is busy, please try again later");
                    return;
                }

                try
                {
                    await _notificationService.MarkAsReadAsync(notificationId, userId);
                    await Clients.Caller.MarkAsReadResult(200, "SUCCESS");
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

        public async Task MarkAllAsRead()
        {
            try
            {
                // Rate limiting
                var userId = GetUserId();
                bool canProceed = true;
                
                lock (_lockObj)
                {
                    if (_lastOperationTime.TryGetValue(userId, out var lastTime))
                    {
                        if ((DateTime.UtcNow - lastTime).TotalMilliseconds < 1000) // 1s cooldown (thao tác nặng)
                        {
                            canProceed = false;
                        }
                    }
                    
                    if (canProceed)
                    {
                        _lastOperationTime[userId] = DateTime.UtcNow;
                    }
                }
                
                if (!canProceed)
                {
                    await Clients.Caller.MarkAllAsReadResult(429, "Rate limit exceeded");
                    return;
                }

                // Semaphore để giới hạn concurrent requests với timeout
                if (!await _markAllAsReadSemaphore.WaitAsync(TimeSpan.FromSeconds(5)))
                {
                    await Clients.Caller.MarkAllAsReadResult(503, "Server is busy, please try again later");
                    return;
                }

                try
                {
                    await _notificationService.MarkAllAsReadAsync(userId);
                    await Clients.Caller.MarkAllAsReadResult(200, "SUCCESS");
                }
                finally
                {
                    _markAllAsReadSemaphore.Release();
                }
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, MARK_ALL_AS_READ_RESULT);
            }
        }

        /// <summary>
        /// Gets the UserId of the connected user
        /// </summary>
        /// <returns></returns>
        private string GetUserId() => Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? throw new UnauthorizedAccessException("User ID not found");

        private IEnumerable<string> GetUserRolesFromClaims()
        {
            var roleClaims = Context.User?.FindAll(ClaimTypes.Role);
            return roleClaims?.Select(c => c.Value) ?? Enumerable.Empty<string>();
        }

        private const string MARK_AS_READ_RESULT = nameof(INotificationClient.MarkAsReadResult);
        private const string MARK_ALL_AS_READ_RESULT = nameof(INotificationClient.MarkAllAsReadResult);

        private async Task HandleExceptionAsync(Exception ex, string resultMethod)
        {
            var (statusCode, errorMessage) = ex switch
            {
                ErrorException errorEx => (errorEx.StatusCode, (object)errorEx.ErrorDetail),
                _ => (500, (object)ex.Message)
            };

            if (ex is ErrorException)
                _logger.LogWarning("Business error in NotificationHub: {Message}", ex.Message);
            else
                _logger.LogError(ex, "Exception in NotificationHub: {Message}", ex.Message);

            Func<Task> clientMethod = resultMethod switch
            {
                MARK_AS_READ_RESULT => () => Clients.Caller.MarkAsReadResult(statusCode, errorMessage),
                MARK_ALL_AS_READ_RESULT => () => Clients.Caller.MarkAllAsReadResult(statusCode, errorMessage),
                _ => throw new ArgumentException($"Unknown result method: {resultMethod}")
            };

            await clientMethod();
        }
    }
}
