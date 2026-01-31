using System.Collections.Concurrent;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

#pragma warning disable CA1848 // Use LoggerMessage delegates for high-performance logging
#pragma warning disable CA1873 // Argument may be expensive - acceptable for scheduler logging

namespace AStar.Dev.OneDrive.Client.Services;

/// <summary>
///     Service for scheduling automatic remote sync checks for accounts.
/// </summary>
public sealed class AutoSyncSchedulerService(IAccountRepository accountRepository, ISyncEngine syncEngine, ILogger<AutoSyncSchedulerService> logger) : IAutoSyncSchedulerService
{
    private readonly ConcurrentDictionary<string, Timer> _timers = new();
    private bool _isDisposed;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting auto-sync scheduler");

        IReadOnlyList<AccountInfo> accounts = await accountRepository.GetAllAsync(cancellationToken);
        foreach(AccountInfo account in accounts)
        {
            if(account.AutoSyncIntervalMinutes.HasValue && account.IsAuthenticated)
                UpdateSchedule(account.AccountId, account.AutoSyncIntervalMinutes.Value);
        }

        logger.LogInformation("Auto-sync scheduler started with {Count} scheduled accounts", _timers.Count);
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        logger.LogInformation("Stopping auto-sync scheduler");

        foreach((var accountId, Timer? timer) in _timers)
        {
            timer.Stop();
            timer.Dispose();
            logger.LogDebug("Stopped timer for account {AccountId}", accountId);
        }

        _timers.Clear();
        logger.LogInformation("Auto-sync scheduler stopped");

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void UpdateSchedule(string accountId, int? intervalMinutes)
    {
        if(_timers.TryRemove(accountId, out Timer? existingTimer))
        {
            existingTimer.Stop();
            existingTimer.Dispose();
            logger.LogDebug("Removed existing timer for account {AccountId}", accountId);
        }

        if(intervalMinutes.HasValue)
        {
            var clampedInterval = Math.Clamp(intervalMinutes.Value, 60, 1440); // 1 hour to 24 hours
            var intervalMs = clampedInterval * 60 * 1000; // Convert to milliseconds

            var timer = new Timer(intervalMs) { AutoReset = true };

            timer.Elapsed += async (sender, e) =>
            {
                try
                {
                    logger.LogInformation("Auto-sync triggered for account {AccountId}", accountId);
                    await syncEngine.StartSyncAsync(accountId, CancellationToken.None);
                }
                catch(Exception ex)
                {
                    logger.LogError(ex, "Auto-sync failed for account {AccountId}", accountId);
                }
            };

            timer.Start();
            _timers[accountId] = timer;

            logger.LogInformation("Scheduled auto-sync for account {AccountId} every {Interval} minutes",
                accountId, clampedInterval);
        }
    }

    /// <inheritdoc />
    public void RemoveSchedule(string accountId)
    {
        if(_timers.TryRemove(accountId, out Timer? timer))
        {
            timer.Stop();
            timer.Dispose();
            logger.LogInformation("Removed auto-sync schedule for account {AccountId}", accountId);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if(_isDisposed)
            return;

        StopAsync().GetAwaiter().GetResult();
        _isDisposed = true;
    }
}
