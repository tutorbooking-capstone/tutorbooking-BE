using App.Repositories.Models.User;

namespace TutorBooking.APIService.Hubs
{
	public class ConnectionMapper
	{
		public static Dictionary<string, ConnectedUser> _connectionMap = new Dictionary<string, ConnectedUser>();

		public static ConnectedUser? Get(string userId)
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

		public static void Set(string userId, ConnectedUser connectedUser)
		{
			_connectionMap[userId] = connectedUser;
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
