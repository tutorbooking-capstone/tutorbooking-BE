namespace TutorBooking.APIService.Hubs
{
    public class ConnectionCleanupService : BackgroundService
    {
        private readonly ConnectionService _connectionService;
        private readonly ILogger<ConnectionCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _connectionTimeout = TimeSpan.FromMinutes(30);

        public ConnectionCleanupService(
            ConnectionService connectionService,
            ILogger<ConnectionCleanupService> logger)
        {
            _connectionService = connectionService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Running connection cleanup job");
                    _connectionService.CleanupInactiveConnections(_connectionTimeout);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during connection cleanup");
                }

                await Task.Delay(_cleanupInterval, stoppingToken);
            }
        }
    }
}
