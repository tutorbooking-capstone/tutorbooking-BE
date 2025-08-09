using App.Core.Base;
using App.Core.Constants;
using App.DTOs.NotificationDTOs;
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

        public NotificationHub(
            INotificationService notificationService, 
            ILogger<NotificationHub> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
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
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task MarkAsRead(string notificationId)
        {
            try
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(notificationId);

                var userId = GetUserId();
                await _notificationService.MarkAsReadAsync(notificationId, userId);
                await Clients.Caller.MarkAsReadResult(200, "SUCCESS");
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

        private IEnumerable<string> GetUserRolesFromClaims()
        {
            var roleClaims = Context.User?.FindAll(ClaimTypes.Role);
            return roleClaims?.Select(c => c.Value) ?? Enumerable.Empty<string>();
        }

        private const string MARK_AS_READ_RESULT = nameof(INotificationClient.MarkAsReadResult);

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
                _ => throw new ArgumentException($"Unknown result method: {resultMethod}")
            };

            await clientMethod();
        }
    }
}
