using App.DTOs.NotificationDTOs;
using App.Repositories.Models.Notifications;
using App.Repositories.Models.Scheduling;
using App.Repositories.Models.User;
using System;
using System.Text.Json;
using static Google.Apis.Requests.BatchRequest;

namespace App.Services.Events
{
    public class NotificationEvents
    {
        // Event triggered when a slot is marked as completed
        public event EventHandler<NotificationToUsersEventArgs>? OnSendNotificationToUserRequested;

        public event EventHandler<NotificationToRolesEventArgs>? OnSendNotificationToRolesRequested;

        public void RequestSendNotificationToUsers(object sender, NotificationToUsersEventArgs e)
        {
            OnSendNotificationToUserRequested?.Invoke(sender, e);
        }

        public void RequestSendNotificationToRoles(object sender, NotificationToRolesEventArgs e)
        {
            OnSendNotificationToRolesRequested?.Invoke(sender, e);
        }

    }

    #region Notification Event Arguments
    public class NotificationToUsersEventArgs : EventArgs
    {
        public NotificationResponse NotificationResponse { get; init; }
        public ICollection<string> ReceiverUserIds { get; init; }
    }

    public class NotificationToRolesEventArgs : EventArgs
    {
        public NotificationResponse NotificationResponse { get; init; }
        public ICollection<Role> Roles { get; init; }
    }
    #endregion
}