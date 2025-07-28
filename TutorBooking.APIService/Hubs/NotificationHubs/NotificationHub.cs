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
            var userId = _userService.GetCurrentUserId();
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
            var userId = _userService.GetCurrentUserId();
            _userIdMapper.Remove(userId);
        }

        public async Task MarkAsRead(string notificationId)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(notificationId, _userService.GetCurrentUserId());
                await Clients.Client(_userIdMapper.GetValueOrDefault(_userService.GetCurrentUserId())).MarkAsReadResult(200, "SUCCESS");
            }
            catch (ErrorException ex)
            {
                _logger.LogError(ex.ToString());
                var connectionId = _userIdMapper.GetValueOrDefault(_userService.GetCurrentUserId());
                if (connectionId != null)
                    await Clients.Client(connectionId).MarkAsReadResult(ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                var connectionId = _userIdMapper.GetValueOrDefault(_userService.GetCurrentUserId());
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
