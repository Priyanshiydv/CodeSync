using CollabService.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace CollabService.Workers
{
    // IHostedService background worker — auto-terminates sessions
    // idle for 30 minutes as required by case study §2.6
    public class SessionCleanupWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<SessionCleanupWorker> _logger;

        // Run cleanup every 5 minutes
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(5);

        // Sessions idle longer than this get terminated
        private readonly TimeSpan _idleTimeout = TimeSpan.FromMinutes(30);

        public SessionCleanupWorker(
            IServiceProvider serviceProvider,
            ILogger<SessionCleanupWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("SessionCleanupWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupInactiveSessionsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during session cleanup.");
                }

                // Wait 5 minutes before next cleanup run
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("SessionCleanupWorker stopped.");
        }

        private async Task CleanupInactiveSessionsAsync()
        {
            // Create a new DI scope for each cleanup run
            // because ICollabService is scoped (not singleton)
            using var scope = _serviceProvider.CreateScope();
            var collabService = scope.ServiceProvider
                .GetRequiredService<ICollabService>();

            var cutoffTime = DateTime.UtcNow - _idleTimeout;

            // Get all currently active sessions
            var activeSessions = await collabService
                .GetAllActiveSessionsAsync();

            int endedCount = 0;

            foreach (var session in activeSessions)
            {
                // End session if no activity since cutoff time
                if (session.LastActivityAt <= cutoffTime)
                {
                    await collabService.EndSessionAsync(
                        session.SessionId.ToString());

                    _logger.LogInformation(
                        "Auto-ended idle session {SessionId}. " +
                        "Last activity: {LastActivity}",
                        session.SessionId,
                        session.LastActivityAt);

                    endedCount++;
                }
            }

            if (endedCount > 0)
            {
                _logger.LogInformation(
                    "Cleanup complete. Ended {Count} idle sessions.",
                    endedCount);
            }
        }
    }
}