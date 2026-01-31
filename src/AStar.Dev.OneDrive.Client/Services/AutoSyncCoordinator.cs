using System.Reactive.Disposables;
using System.Reactive.Linq;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Services;

/// <inheritdoc />
#pragma warning disable CA1848 // Use LoggerMessage delegates
#pragma warning disable CA1873 // Avoid string interpolation in logging
public sealed class AutoSyncCoordinator(IFileWatcherService fileWatcherService, ISyncEngine syncEngine, IAccountRepository accountRepository, ILogger<AutoSyncCoordinator> logger) : IAutoSyncCoordinator
{
    private readonly Dictionary<string, IDisposable> _accountSubscriptions = [];
    private readonly CompositeDisposable _disposables = [];

    /// <summary>
    ///     Starts monitoring an account's sync directory for changes.
    /// </summary>
    /// <param name="accountId">Account identifier.</param>
    /// <param name="localPath">Local sync directory path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    ///     File changes are debounced with a 2-second delay to avoid excessive sync triggers
    ///     when multiple files are changed rapidly.
    /// </remarks>
    public async Task StartMonitoringAsync(string accountId, string localPath, CancellationToken cancellationToken = default)
    {
        StopMonitoring(accountId);

        try
        {
            fileWatcherService.StartWatching(accountId, localPath);

            IDisposable subscription = fileWatcherService.FileChanges
                .Where(e => e.AccountId == accountId)
                .Buffer(TimeSpan.FromSeconds(2))
                .Where(changes => changes.Count > 0)
                .Subscribe(async changes =>
                {
                    logger.LogInformation("Detected {Count} file change(s) for account {AccountId}, triggering sync", changes.Count, accountId);

                    try
                    {
                        await syncEngine.StartSyncAsync(accountId, CancellationToken.None);
                    }
                    catch(Exception ex)
                    {
                        logger.LogError(ex, "Auto-sync failed for account {AccountId}", accountId);
                    }
                });

            _accountSubscriptions[accountId] = subscription;

            logger.LogInformation("Started auto-sync monitoring for account {AccountId} at {Path}",
                accountId, localPath);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "Failed to start auto-sync monitoring for account {AccountId}", accountId);
            throw;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    ///     Stops monitoring an account's sync directory.
    /// </summary>
    /// <param name="accountId">Account identifier.</param>
    public void StopMonitoring(string accountId)
    {
        if(_accountSubscriptions.Remove(accountId, out IDisposable? subscription))
        {
            subscription.Dispose();
            fileWatcherService.StopWatching(accountId);

            logger.LogInformation("Stopped auto-sync monitoring for account {AccountId}", accountId);
        }
    }

    /// <summary>
    ///     Disposes resources and stops all monitoring.
    /// </summary>
    public void Dispose()
    {
        StopAll();
        _disposables.Dispose();
        logger.LogInformation("AutoSyncCoordinator disposed");
    }

    /// <summary>
    ///     Stops monitoring all accounts.
    /// </summary>
    public void StopAll()
    {
        foreach(var accountId in _accountSubscriptions.Keys.ToList()) StopMonitoring(accountId);
    }
}
