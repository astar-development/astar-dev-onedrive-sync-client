using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

public sealed class DeltaPageProcessor(IGraphApiClient graphApiClient, ISyncRepository repo) : IDeltaPageProcessor
{
    public async Task<(DeltaToken finalDelta, int pageCount, int totalItemsProcessed)> ProcessAllDeltaPagesAsync(string accountId, DeltaToken deltaToken, Action<SyncState>? progressReporter,
        CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync(DebugLogMetadata.Services.DeltaPageProcessor.ProcessAllDeltaPagesAsync, accountId, cancellationToken);
        var nextOrDelta = deltaToken.Token;
        DeltaToken finalToken = deltaToken;
        int pageCount = 0, totalItemsProcessed = 0;
        progressReporter?.Invoke(CreateStartingSyncMessage(accountId, string.IsNullOrWhiteSpace(deltaToken.Token)));

        try
        {
            do
            {
                DeltaPage page = await graphApiClient.GetDriveDeltaPageAsync(accountId, nextOrDelta, cancellationToken);
                DriveItemEntity? driveItemRecord = page.Items.FirstOrDefault();
                if(driveItemRecord is not null)
                    deltaToken = new DeltaToken(accountId, driveItemRecord.DriveItemId.Split('!')[0], page.DeltaLink ?? string.Empty, DateTimeOffset.UtcNow);

                totalItemsProcessed += page.Items.Count();
                await DebugLog.InfoAsync(DebugLogMetadata.Services.DeltaPageProcessor.ProcessAllDeltaPagesAsync, accountId, $"Received page: {pageCount} items={page.Items.Count()}", cancellationToken);
                await repo.ApplyDriveItemsAsync(accountId, page.Items, cancellationToken);
                nextOrDelta = page.NextLink;
                if(page.DeltaLink is not null)
                    finalToken = new DeltaToken(accountId, deltaToken!.Id, page.DeltaLink, DateTimeOffset.UtcNow);

                pageCount++;
                await DebugLog.InfoAsync(DebugLogMetadata.Services.DeltaPageProcessor.ProcessAllDeltaPagesAsync, accountId,
                    $"Applied page {pageCount:N0}: items={page.Items.Count():N0} totalItems={totalItemsProcessed:N0} next={page.NextLink is not null}", cancellationToken);
                progressReporter?.Invoke(CreateSyncProgressMessage(accountId, pageCount, totalItemsProcessed, page.NextLink is not null));
            } while(!string.IsNullOrEmpty(nextOrDelta) && !cancellationToken.IsCancellationRequested);

            await DebugLog.InfoAsync(DebugLogMetadata.Services.DeltaPageProcessor.ProcessAllDeltaPagesAsync, accountId,
                $"Delta processing complete: finalToken='***REDACTED***' pageCount={pageCount:N0} totalItems={totalItemsProcessed:N0}", cancellationToken);
        }
        catch(Exception ex)
        {
            await DebugLog.ErrorAsync(DebugLogMetadata.Services.DeltaPageProcessor.ProcessAllDeltaPagesAsync, accountId, $"Exception during delta processing: {ex.Message}", ex, cancellationToken);
            progressReporter?.Invoke(CreateErrorSyncProgress(accountId, totalItemsProcessed, ex.GetBaseException()?.Message ?? "Unknown error"));
            throw new IOException("Error processing delta pages", ex);
        }

        return (finalToken!, pageCount, totalItemsProcessed);
    }

    private static SyncState CreateStartingSyncMessage(string accountId, bool initialSync)
    {
        SyncStatus syncType = SyncStatus.IncrementalDeltaSync;
        var syncMessage = "Starting incremental sync, please wait...";
        if(initialSync)
        {
            syncType = SyncStatus.InitialDeltaSync;
            syncMessage = "Starting initial sync, please wait...";
        }

        return SyncState.Create(accountId, syncType, syncMessage);
    }

    private static SyncState CreateSyncProgressMessage(string accountId, int pageCount, int totalItemsProcessed, bool initialSync)
    {
        SyncStatus syncType = SyncStatus.IncrementalDeltaSync;
        var syncMessage = $"Incremental sync processed page: {pageCount:N0}... total items: {totalItemsProcessed:N0} detected so far";
        if(initialSync)
        {
            syncType = SyncStatus.InitialDeltaSync;
            syncMessage = $"Initial sync processed page: {pageCount:N0}... total items: {totalItemsProcessed:N0} detected so far";
        }

        return SyncState.Create(accountId, syncType, syncMessage);
    }

    private static SyncState CreateErrorSyncProgress(string accountId, int totalItemsProcessed, string errorMessage)
        => SyncState.CreateFailed(accountId, totalItemsProcessed, $"Delta sync failed: {errorMessage}");
}
