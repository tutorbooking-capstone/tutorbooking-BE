using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Repositories.Models
{
	public class Notification
	{
		public string Id { get; set; } = Guid.NewGuid().ToString();
		public ENotificationPriority NotificationPriority { get; set; }
		public string Title { get; set; }
		public string Content { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime DeletedAt { get; set; }
		
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
