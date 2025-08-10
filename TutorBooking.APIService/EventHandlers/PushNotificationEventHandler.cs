using App.Repositories.Models.User;
using App.Services.Events;
using Microsoft.AspNetCore.SignalR;
using TutorBooking.APIService.Hubs.NotificationHubs;

namespace TutorBooking.APIService.EventHandlers
{
    public class PushNotificationEventHandler : IDisposable
    {
        private readonly IHubContext<NotificationHub, INotificationClient> _notificationHubContext;
        private readonly ILogger<PushNotificationEventHandler> _logger;
        private readonly NotificationEvents _notificationEvents;

        // Event Handler Timeout
        private readonly Timer _inactivityTimer;
        private readonly object _timerLock = new object();
        private bool _disposed = false;

        private static readonly TimeSpan InactivityTimeout = TimeSpan.FromSeconds(10);

        public PushNotificationEventHandler(
            NotificationEvents notificationEvents,
            IHubContext<NotificationHub, INotificationClient> notificationHubContext,
            ILogger<PushNotificationEventHandler> logger)
        {
            _notificationHubContext = notificationHubContext;
            _logger = logger;
            _notificationEvents = notificationEvents;

            // Subscribe to event
            _notificationEvents.OnSendNotificationToUserRequested += HandleUserNotificationEvent;
            _notificationEvents.OnSendNotificationToRolesRequested += HandleRoleNotificationEvent;

            // Initialize inactivity timer
            _inactivityTimer = new Timer(OnInactivityTimeout, null, InactivityTimeout, Timeout.InfiniteTimeSpan);
        }

        private async void HandleUserNotificationEvent(object? sender, NotificationToUsersEventArgs e)
        {
            if (_disposed) return;

            ResetInactivityTimer();

            try
            {
                _logger.LogInformation($"Sending '{e.NotificationResponse.Title}' to {e.ReceiverUserIds.Count} users");

                // Send notification to multiple users using Clients.Users
                var userIds = e.ReceiverUserIds.ToList();
                if (userIds.Any())
                {
                    await _notificationHubContext.Clients.Users(userIds)
                        .ReceiveNotification(200, e.NotificationResponse);

                    _logger.LogInformation($"Sent '{e.NotificationResponse.Title}' to {userIds.Count} users");
                }
                else
                {
                    _logger.LogInformation("No users specified to receive the notification.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to users");
            }
        }

        private async void HandleRoleNotificationEvent(object? sender, NotificationToRolesEventArgs e)
        {
            if (_disposed) return;

            ResetInactivityTimer();

            try
            {
                _logger.LogInformation($"Sending '{e.NotificationResponse.Title}' to roles: {string.Join(", ", e.Roles)}");
                foreach (var role in e.Roles)
                {
                    await _notificationHubContext.Clients.Group(role.ToStringRole()).ReceiveNotification(200, e.NotificationResponse);
                }
                _logger.LogInformation($"Sent '{e.NotificationResponse.Title}' to roles: {string.Join(", ", e.Roles)}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to roles");
            }
        }

        #region Inactivity Timer
        private void ResetInactivityTimer()
        {
            if (_disposed) return;

            lock (_timerLock)
            {
                if (!_disposed)
                {
                    _inactivityTimer?.Change(InactivityTimeout, Timeout.InfiniteTimeSpan);
                }
            }
        }
        private void OnInactivityTimeout(object? state)
        {
            _logger.LogInformation("PushNotificationEventHandler disposing due to inactivity timeout");
            Dispose();
        }

        public void Dispose()
        {
            if (_disposed) return;

            lock (_timerLock)
            {
                if (_disposed) return;
                _disposed = true;

                _inactivityTimer?.Dispose();

                _notificationEvents.OnSendNotificationToUserRequested -= HandleUserNotificationEvent;
                _notificationEvents.OnSendNotificationToRolesRequested -= HandleRoleNotificationEvent;

                GC.SuppressFinalize(this);
            }
        }
        #endregion
    }
}