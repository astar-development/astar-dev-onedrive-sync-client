using System.Collections.Concurrent;
using AStar.Dev.OneDrive.Client.Core;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using Timer = System.Timers.Timer;

#pragma warning disable CA1848 // Use LoggerMessage delegates for high-performance logging
#pragma warning disable CA1873 // Argument may be expensive - acceptable for scheduler logging

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

/// <summary>
///     Service for scheduling automatic remote sync checks for accounts.
/// </summary>
public sealed class AutoSyncSchedulerService(IAccountRepository accountRepository, ISyncEngine syncEngine, IDebugLogger debugLogger) : IAutoSyncSchedulerService
{
    private readonly ConcurrentDictionary<string, Timer> _timers = new();
    private bool _isDisposed;

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await debugLogger.LogEntryAsync("Starting auto-sync scheduler", AdminAccountMetadata.AccountId, cancellationToken);
        var autoSyncCount = 0;

        IReadOnlyList<AccountInfo> accounts = await accountRepository.GetAllAsync(cancellationToken);
        foreach(AccountInfo account in accounts)
        {
            if(account is not { AutoSyncIntervalMinutes: > 0, IsAuthenticated: true })
                continue;
            UpdateSchedule(account.AccountId, account.AutoSyncIntervalMinutes);
            autoSyncCount++;
            debugLogger.LogInfoAsync(DebugLogMetadata.Services.AutoSyncSchedulerService.StartAsync, account.AccountId, "Stopping auto-sync scheduler", CancellationToken.None).GetAwaiter().GetResult();
        }

        if(autoSyncCount == 0)
        {
            await debugLogger.LogInfoAsync(DebugLogMetadata.Services.AutoSyncSchedulerService.StartAsync, AdminAccountMetadata.AccountId, "No accounts with auto-sync enabled", cancellationToken);
        }
        else
        {
            await debugLogger.LogInfoAsync(DebugLogMetadata.Services.AutoSyncSchedulerService.StartAsync, AdminAccountMetadata.AccountId, $"Auto-sync scheduler started with {autoSyncCount} scheduled accounts", cancellationToken);
        }

        await debugLogger.LogExitAsync("Auto-sync scheduler started", AdminAccountMetadata.AccountId, cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync()
    {
        debugLogger.LogInfoAsync(DebugLogMetadata.Services.AutoSyncSchedulerService.StopAsync, AdminAccountMetadata.AccountId, "Stopping auto-sync scheduler", CancellationToken.None).GetAwaiter().GetResult();

        foreach((var accountId, Timer? timer) in _timers)
        {
            timer.Stop();
            timer.Dispose();

            debugLogger.LogInfoAsync(DebugLogMetadata.Services.AutoSyncSchedulerService.StopAsync, accountId, "Stopping auto-sync scheduler", CancellationToken.None).GetAwaiter().GetResult();
        }

        _timers.Clear();
        debugLogger.LogExitAsync(DebugLogMetadata.Services.AutoSyncSchedulerService.StopAsync, AdminAccountMetadata.AccountId).GetAwaiter().GetResult();

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public void UpdateSchedule(string accountId, int? intervalMinutes)
    {
        if(_timers.TryRemove(accountId, out Timer? existingTimer))
        {
            existingTimer.Stop();
            existingTimer.Dispose();
            debugLogger.LogInfoAsync(DebugLogMetadata.Services.AutoSyncSchedulerService.UpdateSchedule, accountId, "Removed existing timer for account", CancellationToken.None).GetAwaiter().GetResult();
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
                    await debugLogger.LogInfoAsync(DebugLogMetadata.Services.AutoSyncSchedulerService.AutoSyncTriggered, accountId, "Auto-sync triggered", CancellationToken.None);
                    await syncEngine.StartSyncAsync(accountId, CancellationToken.None);
                }
                catch(Exception ex)
                {
                    debugLogger.LogErrorAsync(DebugLogMetadata.Services.AutoSyncSchedulerService.AutoSyncTriggered, $"Scheduled auto-sync encountered {ex.GetBaseException().Message}", accountId, ex, CancellationToken.None).GetAwaiter().GetResult();
                }
            };

            timer.Start();
            _timers[accountId] = timer;

            debugLogger.LogInfoAsync(DebugLogMetadata.Services.AutoSyncSchedulerService.AutoSyncTriggered, accountId, $"Scheduled auto-sync for account {accountId} every {clampedInterval} minutes", CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    /// <inheritdoc />
    public void RemoveSchedule(string accountId)
    {
        if(_timers.TryRemove(accountId, out Timer? timer))
        {
            timer.Stop();
            timer.Dispose();
            debugLogger.LogInfoAsync(DebugLogMetadata.Services.AutoSyncSchedulerService.RemoveSchedule, accountId, "Removed auto-sync schedule for account", CancellationToken.None).GetAwaiter().GetResult();
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
