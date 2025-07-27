using App.Core.Base;
using App.Core.Constants;
using App.DTOs.NotificationDTOs;
using App.Repositories.Models.User;
using App.Services.Interfaces;
using App.Services.Services;
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
	[AllowAnonymous]
	public class NotificationHub : Hub<INotificationClient>
	{
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationHub> _logger;

        // UserId <-> ConnectionId
        public static Dictionary<string, string> _userIdMapper = new Dictionary<string, string>();

        public NotificationHub(INotificationService notificationService, ILogger<NotificationHub> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
                await base.OnConnectedAsync();
                var userId = GetUserId();
                var roles = GetRole();
                _userIdMapper.Remove(userId);
                _userIdMapper.TryAdd(userId, Context.ConnectionId);
                foreach (var role in GetRole())
                    await Groups.AddToGroupAsync(Context.ConnectionId, role.ToStringRole());
                await Clients.Client(Context.ConnectionId).UserConnected("CONNECTED_TO_NOTIFICATION_HUB");
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			await base.OnDisconnectedAsync(exception);
		}

        public async Task MarkAsRead(string notificationId)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(notificationId, GetUserId());
                await Clients.Client(_userIdMapper.GetValueOrDefault(GetUserId())).MarkAsReadResult(200, "SUCCESS");
            }
            catch (ErrorException ex)
            {
                _logger.LogError(ex.ToString());
                var connectionId = _userIdMapper.GetValueOrDefault(GetUserId());
                if (connectionId != null)
                    await Clients.Client(connectionId).MarkAsReadResult(ex.StatusCode, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                var connectionId = _userIdMapper.GetValueOrDefault(GetUserId());
                if (connectionId != null)
                    await Clients.Client(connectionId).MarkAsReadResult(500, ex.Message);
            }   
        }


        private string GetUserId()
        {
            try
            {
                var token = Context.GetHttpContext().Request.Headers[HeaderNames.Authorization].ToString().Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var securityToken = handler.ReadJwtToken(token);
                var userId = securityToken.Claims.FirstOrDefault(c => c.Type.Equals(JwtRegisteredClaimNames.Sub)).Value;
                return userId;
            } 
            catch (Exception ex)
            {
                throw new Exception("FAILED_TO_GET_USER_ID_FROM_TOKEN");
            }      
        }

        private List<Role> GetRole()
        {
            try
            {
                var token = Context.GetHttpContext().Request.Headers[HeaderNames.Authorization].ToString().Substring("Bearer ".Length).Trim();
                var handler = new JwtSecurityTokenHandler();
                var securityToken = handler.ReadJwtToken(token);
                var roles = securityToken.Claims.Where(c => c.Type.Equals(ClaimTypes.Role)).Select(x => x.Value.ToRoleEnum()).ToList();
                return roles;
            }
            catch (Exception ex)
            {
                throw new Exception( "FAILED_TO_GET_ROLE_FROM_TOKEN");
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
