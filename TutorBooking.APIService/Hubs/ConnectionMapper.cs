using App.Repositories.Models.User;

namespace TutorBooking.APIService.Hubs
{
	public class ConnectionMapper
	{
		public static Dictionary<string, ConnectedUser> _connectionMap = new Dictionary<string, ConnectedUser>();


		public static ConnectedUser? GetConnectedUser(string userId)
		{
			return _connectionMap[userId];
		}

		public static void SetConnectedUser(string userId, ConnectedUser connectedUser)
		{
			_connectionMap[userId] = connectedUser;
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
		public Role Role { get; set; }
	}
}
