using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.DTOs;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

public sealed class DeltaPageProcessor(IGraphApiClient graphApiClient, ISyncRepository repo) : IDeltaPageProcessor
{
    public async Task<(DeltaToken finalDelta, int pageCount, int totalItemsProcessed)> ProcessAllDeltaPagesAsync(string accountId, DeltaToken? deltaToken, Action<SyncState>? progressCallback,
        CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync(DebugLogMetadata.Services.DeltaPageProcessor.ProcessAllDeltaPagesAsync, cancellationToken);
        var nextOrDelta = deltaToken?.Token;
        DeltaToken finalToken = deltaToken;
        int pageCount = 0, totalItemsProcessed = 0;
        progressCallback?.Invoke(CreateStartingSyncMessage(accountId, deltaToken == null));

        try
        {
            do
            {
                DeltaPage page = await graphApiClient.GetDriveDeltaPageAsync(accountId, nextOrDelta, cancellationToken);
                DriveItemRecord? driveItemRecord = page.Items.FirstOrDefault();
                if(driveItemRecord is not null) deltaToken = new DeltaToken(accountId, driveItemRecord.Id.Split('!')[0], page.DeltaLink ?? string.Empty, DateTimeOffset.UtcNow);

                totalItemsProcessed += page.Items.Count();
                await DebugLog.InfoAsync(DebugLogMetadata.Services.DeltaPageProcessor.ProcessAllDeltaPagesAsync, $"Received page: {pageCount} items={page.Items.Count()}", cancellationToken);
                await repo.ApplyDriveItemsAsync(accountId, page.Items, cancellationToken);
                nextOrDelta = page.NextLink;
                if(page.DeltaLink is not null) finalToken = new DeltaToken(accountId, deltaToken!.Id, page.DeltaLink, DateTimeOffset.UtcNow);

                pageCount++;
                await DebugLog.InfoAsync(DebugLogMetadata.Services.DeltaPageProcessor.ProcessAllDeltaPagesAsync,
                    $"Applied page {pageCount}: items={page.Items.Count()} totalItems={totalItemsProcessed} next={page.NextLink is not null}", cancellationToken);
                progressCallback?.Invoke(CreateSyncProgressMessage(accountId, pageCount, totalItemsProcessed, page.NextLink is not null));
            } while(!string.IsNullOrEmpty(nextOrDelta) && !cancellationToken.IsCancellationRequested);

            await DebugLog.InfoAsync(DebugLogMetadata.Services.DeltaPageProcessor.ProcessAllDeltaPagesAsync,
                $"Delta processing complete: finalToken='***REDACTED***' pageCount={pageCount} totalItems={totalItemsProcessed}", cancellationToken);
        }
        catch(Exception ex)
        {
            await DebugLog.ErrorAsync(DebugLogMetadata.Services.DeltaPageProcessor.ProcessAllDeltaPagesAsync, $"Exception during delta processing: {ex.Message}", ex, cancellationToken);
            progressCallback?.Invoke(CreateErrorSyncProgress(accountId, totalItemsProcessed, ex.GetBaseException()?.Message ?? "Unknown error"));
            throw new IOException("Error processing delta pages", ex);
        }

        if(finalToken is not null)
            finalToken = finalToken with { AccountId = accountId };

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

        return new SyncState(accountId, syncType, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, syncMessage);
    }

    private static SyncState CreateSyncProgressMessage(string accountId, int pageCount, int totalItemsProcessed, bool initialSync)
    {
        SyncStatus syncType = SyncStatus.IncrementalDeltaSync;
        var syncMessage = $"Incremental sync processed page: {pageCount}... total items: {totalItemsProcessed} detected so far";
        if(initialSync)
        {
            syncType = SyncStatus.InitialDeltaSync;
            syncMessage = $"Initial sync processed page: {pageCount}... total items: {totalItemsProcessed} detected so far";
        }

        return new SyncState(accountId, syncType, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, syncMessage);
    }

    private static SyncState CreateErrorSyncProgress(string accountId, int totalItemsProcessed, string errorMessage)
        => new(accountId, SyncStatus.Failed, totalItemsProcessed, 0, 0, 0, 0, 0, 0, 0, 0, 0, $"Delta sync failed: {errorMessage}", DateTimeOffset.UtcNow);
}
