using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Service for managing delta token storage and processing delta queries from OneDrive.
/// </summary>
public sealed class DeltaProcessingService(ISyncRepository syncRepository, IDeltaPageProcessor deltaPageProcessor) : IDeltaProcessingService
{
    /// <inheritdoc />
    public async Task<DeltaToken?> GetDeltaTokenAsync(string accountId, HashedAccountId hashedAccountId, CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync(DebugLogMetadata.Services.DeltaProcessingService.GetDeltaTokenAsync, hashedAccountId, cancellationToken);

        DeltaToken? token = await syncRepository.GetDeltaTokenAsync(accountId, cancellationToken);

        _ = await DebugLog.LogInfoAsync(DebugLogMetadata.Services.DeltaProcessingService.GetDeltaTokenAsync, hashedAccountId, token != null ? $"Retrieved delta token for account {hashedAccountId}" : $"No delta token found for account {hashedAccountId}", cancellationToken);

        return token;
    }

    /// <inheritdoc />
    public async Task SaveDeltaTokenAsync(DeltaToken token, HashedAccountId hashedAccountId, CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync(DebugLogMetadata.Services.DeltaProcessingService.SaveDeltaTokenAsync, hashedAccountId, cancellationToken);

        await syncRepository.SaveOrUpdateDeltaTokenAsync(token, cancellationToken);

        _ = await DebugLog.LogInfoAsync(DebugLogMetadata.Services.DeltaProcessingService.SaveDeltaTokenAsync, hashedAccountId, $"Saved delta token for account {hashedAccountId}", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(DeltaToken finalToken, int pageCount, int totalItemsProcessed)> ProcessDeltaPagesAsync(string accountId, HashedAccountId hashedAccountId, DeltaToken? deltaToken, Action<SyncState>? progressCallback,
        CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync(DebugLogMetadata.Services.DeltaProcessingService.ProcessDeltaPagesAsync, hashedAccountId, cancellationToken);

        DeltaToken tokenToUse = deltaToken ?? new DeltaToken(accountId, hashedAccountId, string.Empty, string.Empty, DateTimeOffset.UtcNow);

        _ = await DebugLog.LogInfoAsync(DebugLogMetadata.Services.DeltaProcessingService.ProcessDeltaPagesAsync, hashedAccountId, $"Starting delta page processing for account {hashedAccountId}, initialSync={string.IsNullOrEmpty(tokenToUse.Token)}", cancellationToken);

        (DeltaToken? finalDelta, var pageCount, var totalItemsProcessed) =
            await deltaPageProcessor.ProcessAllDeltaPagesAsync(accountId, hashedAccountId, tokenToUse, progressCallback, cancellationToken);

        _ = await DebugLog.LogInfoAsync(DebugLogMetadata.Services.DeltaProcessingService.ProcessDeltaPagesAsync, hashedAccountId, $"Completed delta page processing: pageCount={pageCount}, totalItems={totalItemsProcessed}", cancellationToken);

        return (finalDelta, pageCount, totalItemsProcessed);
    }
}
