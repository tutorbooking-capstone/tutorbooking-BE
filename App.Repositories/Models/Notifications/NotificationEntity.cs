using App.Repositories.Models.User;

namespace App.Repositories.Models.Notifications
{
	public class NotificationEntity
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public ENotificationPriority NotificationPriority { get; set; }
		public string Title { get; set; }
		public string Content { get; set; }
		public string? AdditionalData { get; set; }
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime DeletedAt { get; set; }
		public virtual ICollection<AppUser>? AppUsers { get; set; }
		public virtual ICollection<AppUserNotification>? AppUserNotifications { get; set; }
		
	}

	public enum ENotificationPriority
	{
		Low,
		Normal,
		Warning,
		Critical
	}

	public enum ENotificationCategory
	{
		System,
		Alert,
		Information,
		Marketing,
		Reminder,
		Authentication
	}
}
