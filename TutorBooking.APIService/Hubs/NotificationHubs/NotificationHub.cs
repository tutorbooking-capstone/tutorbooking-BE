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

namespace TutorBooking.APIService.Hubs.NotificationHubs
{
    [Authorize]
    public class NotificationHub : Hub<INotificationClient>
    {
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly ILogger<NotificationHub> _logger;

        // UserId <-> ConnectionId
        public static Dictionary<string, string> _userIdMapper = new Dictionary<string, string>();

        public NotificationHub(INotificationService notificationService, ILogger<NotificationHub> logger, IUserService userService)
        {
            _notificationService = notificationService;
            _logger = logger;
            _userService = userService;
        }

        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in Hub Context for connection {ConnectionId}. Aborting connection.", Context.ConnectionId);
                Context.Abort(); // Ngắt kết nối nếu không xác thực được
                return;
            }

            var roles = await _userService.GetUserRolesAsync(userId);
            _userIdMapper.Remove(userId);
            _userIdMapper.TryAdd(userId, Context.ConnectionId);
            foreach (var role in roles)
                await Groups.AddToGroupAsync(Context.ConnectionId, role);
            await Clients.Client(Context.ConnectionId).UserConnected("CONNECTED_TO_NOTIFICATION_HUB");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
            // Lấy userId từ mapper thay vì context vì context có thể không còn đáng tin cậy
            var userId = _userIdMapper.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if(!string.IsNullOrEmpty(userId))
            {
                _userIdMapper.Remove(userId);
            }
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

                var connectionId = _userIdMapper.GetValueOrDefault(userId);
                if(connectionId != null)
                    await Clients.Client(connectionId).MarkAsReadResult(200, "SUCCESS");
            }
            catch (ErrorException ex)
            {
                _logger.LogError(ex.ToString());
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if(userId == null) return;
                var connectionId = _userIdMapper.GetValueOrDefault(userId);
                if (connectionId != null)
                    await Clients.Client(connectionId).MarkAsReadResult(ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if(userId == null) return;
                var connectionId = _userIdMapper.GetValueOrDefault(userId);
                if (connectionId != null)
                    await Clients.Client(connectionId).MarkAsReadResult(500, ex.Message);
            }
        }
    }

    public static class NotificationHubExtensions
    {
        public static async Task SendNotificationToUsersAsync(this IHubContext<NotificationHub, INotificationClient> hubContext, INotificationService notificationService, SendNotificationToUsersRequest request)
        {
            var response = await notificationService.CreateForUsersAsync(request);

            var connectionIds = new List<string>();
            foreach (var user in request.ReceiverUserIds)
            {
                string cId = NotificationHub._userIdMapper.FirstOrDefault(x => x.Key.Equals(user)).Value;
                if (cId != null)
                    connectionIds.Add(cId);
            }
            await hubContext.Clients.Clients(connectionIds).ReceiveNotification(200, response);
        }
    }
}
