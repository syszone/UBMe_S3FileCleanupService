using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using S3FileCleanup.Shared.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace S3FileCleanup.Service
{
    public class Worker : BackgroundService
    {
        private readonly FileDeletionService _fileDeletionService;
        private readonly ILogger<Worker> _logger;
        private readonly int _cleanupIntervalHours;
        private readonly IHostApplicationLifetime _lifetime;

        public Worker(FileDeletionService fileDeletionService, ILogger<Worker> logger, IConfiguration configuration, IHostApplicationLifetime lifetime)
        {
            _fileDeletionService = fileDeletionService ?? throw new ArgumentNullException(nameof(fileDeletionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));

            // Retrieve cleanup interval from configuration
            _cleanupIntervalHours = int.TryParse(configuration["WorkerSettings:CleanupIntervalHours"], out var interval)
                ? interval
                : 24; // Default to 24 hours if not configured
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("File cleanup service started at: {Time}", DateTime.UtcNow);

            // Notify that the service has started
            _lifetime.ApplicationStarted.Register(() =>
            {
                _logger.LogInformation("Service has successfully started and is now running.");
            });

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("File cleanup started at: {Time}", DateTime.UtcNow);

                    // Execute the cleanup logic
                    await _fileDeletionService.DeleteOldFilesAsync();

                    _logger.LogInformation("File cleanup completed at: {Time}", DateTime.UtcNow);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred during the file cleanup process at: {Time}", DateTime.UtcNow);
                }

                // Wait for the configured interval
                try
                {
                    _logger.LogInformation("Waiting for the next cleanup cycle (Interval: {Interval} hours)...", _cleanupIntervalHours);
                    await Task.Delay(TimeSpan.FromHours(_cleanupIntervalHours), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("File cleanup service is stopping due to cancellation.");
                    break;
                }
            }

            _logger.LogInformation("File cleanup service stopped at: {Time}", DateTime.UtcNow);
        }
    }
}
