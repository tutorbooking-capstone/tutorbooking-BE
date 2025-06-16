using App.Repositories.Models.User;

namespace TutorBooking.APIService.Hubs
{
	public class ConnectionMapper
	{
		public static Dictionary<string, string> _connectionMap = new Dictionary<string, string>();

		public static string? Get(string? userId)
		{
			try
			{
				return _connectionMap[userId];
			}
			catch (Exception ex)
			{
				return null;
			}
		}

		public static void Set(string userId, string connectionId)
		{
			_connectionMap[userId] = connectionId;
		}

		public static bool Contains(string userId)
		{
			return _connectionMap.ContainsKey(userId);
		}

		public static void RemoveConnectedUser(string userId)
		{
			_connectionMap.Remove(userId);
		}

	}

	public class ConnectedUser
	{
		public string UserId { get; set; }
		public string ConnectionId { get; set; }
	}
}
