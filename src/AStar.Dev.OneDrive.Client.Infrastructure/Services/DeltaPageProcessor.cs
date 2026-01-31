using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.DTOs;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

public sealed class DeltaPageProcessor(IGraphApiClient graphApiClient, ISyncRepository repo, ILogger<DeltaPageProcessor> logger) : IDeltaPageProcessor
{
    public async Task<(DeltaToken finalDelta, int pageCount, int totalItemsProcessed)> ProcessAllDeltaPagesAsync(string accountId, DeltaToken? deltaToken, Action<SyncState>? progressCallback, CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync("FileProcessor.ProcessFileAsync", cancellationToken);
        logger.LogInformation("[DeltaPageProcessor] Starting delta page processing (with progress callback)");
        string? nextOrDelta = null;
        DeltaToken finalToken = deltaToken;
        int pageCount = 0, totalItemsProcessed = 0;
        try
        {
            do
            {
                logger.LogDebug("[DeltaPageProcessor] Requesting delta page: nextOrDelta={NextOrDelta}", nextOrDelta);
                DeltaPage page = await graphApiClient.GetDriveDeltaPageAsync(accountId, nextOrDelta, cancellationToken);
                DriveItemRecord? driveItemRecord = page.Items.FirstOrDefault();
                if(driveItemRecord is not null)
                {
                    deltaToken = new DeltaToken("PlaceholderAccountId", driveItemRecord.Id.Split('!')[0], page.DeltaLink ?? string.Empty, DateTimeOffset.UtcNow);
                }

                logger.LogDebug("[DeltaPageProcessor] Received page: items={Count} nextLink={NextLink} deltaLink={DeltaLink}", page.Items.Count(), page.NextLink, page.DeltaLink);
                await repo.ApplyDriveItemsAsync(accountId, page.Items, cancellationToken);
                totalItemsProcessed += page.Items.Count();
                nextOrDelta = page.NextLink;
                if(page.DeltaLink is not null)
                {
                    finalToken = new("PlaceholderAccountId", deltaToken!.Id, page.DeltaLink, DateTimeOffset.UtcNow);
                }

                pageCount++;
                logger.LogInformation("[DeltaPageProcessor] Applied page {PageNum}: items={Count} totalItems={Total} next={Next}",
                    pageCount, page.Items.Count(), totalItemsProcessed, page.NextLink is not null);
                progressCallback?.Invoke(CreateSyncProgressMessage(accountId,pageCount, totalItemsProcessed, page));
                if(pageCount > 10000)
                {
                    logger.LogWarning("[DeltaPageProcessor] Exceeded max page count (10000), aborting to prevent infinite loop.");
                    break;
                }
            } while(!string.IsNullOrEmpty(nextOrDelta) && !cancellationToken.IsCancellationRequested);
            logger.LogInformation("[DeltaPageProcessor] Delta processing complete: finalToken={FinalToken} pageCount={PageCount} totalItems={TotalItems}", finalToken, pageCount, totalItemsProcessed);
        }
        catch(Exception ex)
        {
            logger.LogError(ex, "[DeltaPageProcessor] Exception during delta processing: {Message}", ex.Message);
            progressCallback?.Invoke(CreateErrorSyncProgress(accountId,totalItemsProcessed, ex.GetBaseException()?.Message ?? "Unknown error"));
            throw new IOException("Error processing delta pages", ex);
        }

        return (finalToken!, pageCount, totalItemsProcessed);
    }

    private static SyncState CreateSyncProgressMessage(string accountId,int pageCount, int totalItemsProcessed, DeltaPage page) => new(accountId, Core.Models.Enums.SyncStatus.Running, totalItemsProcessed, 0, 0, 0, 0, 0, 0, 0, 0, 0, $"Delta page {pageCount} applied ({page.Items.Count()} items)");
    

    private static SyncState CreateErrorSyncProgress(string accountId,int totalItemsProcessed, string errorMessage) => new(accountId, Core.Models.Enums.SyncStatus.Failed, totalItemsProcessed, 0, 0, 0, 0, 0, 0, 0, 0, 0,$"Delta sync failed: {errorMessage}", DateTimeOffset.UtcNow);
}
