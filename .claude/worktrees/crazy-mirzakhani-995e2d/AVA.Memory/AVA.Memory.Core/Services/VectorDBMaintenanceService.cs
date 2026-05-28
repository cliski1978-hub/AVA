using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AVA.Memory.Core.Services
{
    /// <summary>
    /// Background hosted service that periodically runs
    /// VectorDB maintenance tasks using <see cref="VectorDBMaintenanceContext"/>.
    /// </summary>
    public sealed class VectorDBMaintenanceService : BackgroundService
    {
        private readonly VectorDBMaintenanceContext _context;
        private readonly ILogger<VectorDBMaintenanceService>? _logger;

        // Interval between maintenance cycles (default: every 30 minutes)
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(30);

        public VectorDBMaintenanceService(
            VectorDBMaintenanceContext context,
            ILogger<VectorDBMaintenanceService>? logger = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger;
        }

        /// <summary>
        /// Core background loop.
        /// </summary>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger?.LogInformation("[VectorDB] Maintenance service started. Interval: {Interval} minutes.", _interval.TotalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _context.RunMaintenanceAsync(stoppingToken);

                    var report = _context.GetLastReport();
                    _logger?.LogInformation("[VectorDB] Maintenance completed at {Time}. {Count} collections processed.",
                        _context.LastRunUtc, report.Count);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "[VectorDB] Maintenance cycle failed: {Message}", ex.Message);
                }

                try
                {
                    await Task.Delay(_interval, stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // normal exit
                    break;
                }
            }

            _logger?.LogInformation("[VectorDB] Maintenance service stopped.");
        }
    }
}
