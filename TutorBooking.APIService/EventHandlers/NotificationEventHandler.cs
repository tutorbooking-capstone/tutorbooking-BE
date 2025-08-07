using App.Core.Constants;
using App.DTOs.NotificationDTOs;
using App.Repositories.Models.Notifications;
using App.Repositories.Models.User;
using App.Services.Events;
using App.Services.Interfaces;
using App.Services.Services;
using Microsoft.AspNetCore.SignalR;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Text.Json;
using TutorBooking.APIService.Hubs;
using TutorBooking.APIService.Hubs.NotificationHubs;

namespace TutorBooking.APIService.EventHandlers
{
    public class NotificationEventHandler
    {
        private readonly IHubContext<NotificationHub, INotificationClient> _notificationHubContext;
        private readonly ConnectionService _connectionService;
        private readonly ILogger<NotificationEventHandler> _logger;

        public NotificationEventHandler(
            NotificationEvents notificationEvents,
            IHubContext<NotificationHub, INotificationClient> notificationHubContext,
            ConnectionService connectionService,
            ILogger<NotificationEventHandler> logger)
        {
            _notificationHubContext = notificationHubContext;
            _connectionService = connectionService;
            _logger = logger;

            // Subscribe to event
            notificationEvents.OnSendNotificationToUserRequested += HandleUserNotificationEvent;
            notificationEvents.OnSendNotificationToRolesRequested += HandleRoleNotificationEvent;
        }

        private async void HandleUserNotificationEvent(object? sender, NotificationToUsersEventArgs e)
        {
            try
            {
                _logger.LogInformation($"Sent '{e.NotificationResponse.Title}' to {e.ReceiverUserIds.Count} users");

                var connectionIds = e.ReceiverUserIds
                .Select(userId => _connectionService.GetConnectionId(userId))
                .Where(connectionId => !string.IsNullOrEmpty(connectionId))
                .ToList();

                if (connectionIds.Any())
                    await _notificationHubContext.Clients.Clients(
                        (IReadOnlyList<string>)connectionIds
                        .Where(x => x != null).ToList())
                        .ReceiveNotification(200, e.NotificationResponse);
                _logger.LogInformation($"Sent '{e.NotificationResponse.Title}' to {e.ReceiverUserIds.Count} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
            }
        }

        private async void HandleRoleNotificationEvent(object? sender, NotificationToRolesEventArgs e)
        {
            try
            {
                foreach (var role in e.Roles)
                    await _notificationHubContext.Clients.Group(role.ToStringRole()).ReceiveNotification(200, e.NotificationResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to roles");
            }
        }
    }
}