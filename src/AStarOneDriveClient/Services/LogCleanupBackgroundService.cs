using AStarOneDriveClient.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AStarOneDriveClient.Services;

/// <summary>
///     Background service to clean up old SyncSessionLogs and DebugLogs entries.
/// </summary>
public sealed class LogCleanupBackgroundService(IServiceProvider serviceProvider, ILogger<LogCleanupBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(12); // Run twice a day
    private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(14);
    private readonly IServiceScopeFactory _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while(!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using IServiceScope scope = _scopeFactory.CreateScope();
                SyncDbContext db = scope.ServiceProvider.GetRequiredService<SyncDbContext>();
                DateTime cutoff = DateTime.UtcNow - RetentionPeriod;

                var sessionLogsDeleted = await db.SyncSessionLogs
                    .Where(x => x.StartedUtc < cutoff)
                    .ExecuteDeleteAsync(stoppingToken);

                var debugLogsDeleted = await db.DebugLogs
                    .Where(x => x.TimestampUtc < cutoff)
                    .ExecuteDeleteAsync(stoppingToken);

                logger.LogInformation("LogCleanupBackgroundService: Deleted {SessionLogs} session logs and {DebugLogs} debug logs older than {Cutoff}", sessionLogsDeleted, debugLogsDeleted, cutoff);
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "LogCleanupBackgroundService: Error during cleanup");
            }

            try
            {
                await Task.Delay(CleanupInterval, stoppingToken);
            }
            catch(TaskCanceledException)
            {
                // Service is stopping
                break;
            }
        }
    }
}
