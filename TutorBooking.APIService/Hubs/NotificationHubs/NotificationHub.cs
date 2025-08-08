using App.Core.Base;
using App.Core.Constants;
using App.DTOs.NotificationDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using App.Services.Interfaces.User;
using App.Services.Services;
using App.Services.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Net.Http.Headers;
using Org.BouncyCastle.Asn1.Ocsp;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static Google.Apis.Requests.BatchRequest;
using System.Collections.Concurrent;

namespace TutorBooking.APIService.Hubs.NotificationHubs
{
    [Authorize]
    public class NotificationHub : Hub<INotificationClient>
    {
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly ILogger<NotificationHub> _logger;
        private readonly ConnectionService _connectionService;

        public NotificationHub(
            INotificationService notificationService, 
            ILogger<NotificationHub> logger, 
            IUserService userService,
            ConnectionService connectionService)
        {
            _notificationService = notificationService;
            _logger = logger;
            _userService = userService;
            _connectionService = connectionService;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in Hub Context. Aborting connection.");
                Context.Abort();
                return;
            }

            var roles = await _userService.GetUserRolesAsync(userId);
            _connectionService.AddConnection(userId, Context.ConnectionId, roles.ToList());

            // Thêm user vào các group dựa trên role
            foreach (var role in roles)
                await Groups.AddToGroupAsync(Context.ConnectionId, role);
                
            await Clients.Caller.UserConnected("CONNECTED_TO_NOTIFICATION_HUB");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                _connectionService.RemoveConnection(userId);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task MarkAsRead(string notificationId)
        {
            try
            {
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    throw new ErrorException(StatusCodes.Status401Unauthorized, ErrorCode.Unauthorized, "User not authenticated");
                }

                await _notificationService.MarkAsReadAsync(notificationId, userId);
                await Clients.Caller.MarkAsReadResult(200, "SUCCESS");
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex);
            }
        }

        private async Task HandleExceptionAsync(Exception ex)
        {
            int statusCode = 500;
            object errorMessage = ex.Message;

            if (ex is ErrorException errorEx)
            {
                statusCode = errorEx.StatusCode;
                errorMessage = errorEx.ErrorDetail;
                _logger.LogWarning($"Business error in NotificationHub: {errorEx.Message}");
            }
            else
            {
                _logger.LogError(ex, $"Exception in NotificationHub: {ex.Message}");
            }

            await Clients.Caller.MarkAsReadResult(statusCode, errorMessage);
        }
    }
}
