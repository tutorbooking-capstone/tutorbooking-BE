using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace TutorBooking.APIService.Hubs
{
    public class ConnectionServiceOptions
    {
        public int MaxConnectionsPerUser { get; set; } = 5;
        public int MaxTotalConnections { get; set; } = 1000;
    }
    
    public class ConnectionService
    {
        private static readonly ConcurrentDictionary<string, UserConnection> _connections = new();
        private readonly ILogger<ConnectionService> _logger;
        private readonly IOptions<ConnectionServiceOptions> _options;

        public ConnectionService(ILogger<ConnectionService> logger, IOptions<ConnectionServiceOptions> options)
        {
            _logger = logger;
            _options = options;
        }

        public void AddConnection(string userId, string connectionId, List<string>? roles = null)
        {
            // Kiểm tra giới hạn kết nối
            if (_connections.Count >= _options.Value.MaxTotalConnections)
                throw new HubException("Hệ thống đã đạt giới hạn kết nối tối đa");

            // Đếm số kết nối hiện tại của user
            int userConnections = _connections.Count(x => x.Key == userId);
            if (userConnections >= _options.Value.MaxConnectionsPerUser)
                throw new HubException("Đã đạt giới hạn số kết nối cho một người dùng");
            
            var connection = new UserConnection 
            { 
                ConnectionId = connectionId,
                LastActivity = DateTime.UtcNow,
                Roles = roles ?? new List<string>()
            };
            
            _connections.AddOrUpdate(userId, connection, (_, _) => connection);
        }

        public string? GetConnectionId(string userId)
        {
            return _connections.TryGetValue(userId, out var connection) ? connection.ConnectionId : null;
        }

        public void RemoveConnection(string userId)
        {
            _connections.TryRemove(userId, out _);
        }

        public bool IsConnected(string userId)
        {
            return _connections.ContainsKey(userId);
        }

        // Dọn dẹp các kết nối cũ
        public void CleanupInactiveConnections(TimeSpan timeout)
        {
            var cutoff = DateTime.UtcNow.Subtract(timeout);
            var inactiveUsers = _connections.Where(x => x.Value.LastActivity < cutoff).Select(x => x.Key).ToList();
            
            foreach (var userId in inactiveUsers)
            {
                _connections.TryRemove(userId, out _);
                _logger.LogInformation($"Removed inactive connection for user {userId}");
            }
        }

        public class UserConnection
        {
            public required string ConnectionId { get; set; }
            public DateTime LastActivity { get; set; }
            public List<string> Roles { get; set; } = new();
        }
    }
}
