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
    public async Task<DeltaToken?> GetDeltaTokenAsync(string accountId, CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync(DebugLogMetadata.Services.DeltaProcessingService.GetDeltaTokenAsync, accountId, cancellationToken);

        DeltaToken? token = await syncRepository.GetDeltaTokenAsync(accountId, cancellationToken);

        await DebugLog.InfoAsync(
            DebugLogMetadata.Services.DeltaProcessingService.GetDeltaTokenAsync,
            accountId,
            token != null ? $"Retrieved delta token for account {accountId}" : $"No delta token found for account {accountId}",
            cancellationToken);

        return token;
    }

    /// <inheritdoc />
    public async Task SaveDeltaTokenAsync(DeltaToken token, CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync(DebugLogMetadata.Services.DeltaProcessingService.SaveDeltaTokenAsync, token.AccountId, cancellationToken);

        await syncRepository.SaveOrUpdateDeltaTokenAsync(token, cancellationToken);

        await DebugLog.InfoAsync(
            DebugLogMetadata.Services.DeltaProcessingService.SaveDeltaTokenAsync,
            token.AccountId,
            $"Saved delta token for account {token.AccountId}",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(DeltaToken finalToken, int pageCount, int totalItemsProcessed)> ProcessDeltaPagesAsync(string accountId, HashedAccountId hashedAccountId, DeltaToken? deltaToken, Action<SyncState>? progressCallback,
        CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync(DebugLogMetadata.Services.DeltaProcessingService.ProcessDeltaPagesAsync, accountId, cancellationToken);

        DeltaToken tokenToUse = deltaToken ?? new DeltaToken(accountId, hashedAccountId, string.Empty, string.Empty, DateTimeOffset.UtcNow);

        await DebugLog.InfoAsync(
            DebugLogMetadata.Services.DeltaProcessingService.ProcessDeltaPagesAsync,
            accountId,
            $"Starting delta page processing for account {accountId}, initialSync={string.IsNullOrEmpty(tokenToUse.Token)}",
            cancellationToken);

        (DeltaToken? finalDelta, var pageCount, var totalItemsProcessed) =
            await deltaPageProcessor.ProcessAllDeltaPagesAsync(accountId, hashedAccountId, tokenToUse, progressCallback, cancellationToken);

        await DebugLog.InfoAsync(
            DebugLogMetadata.Services.DeltaProcessingService.ProcessDeltaPagesAsync,
            accountId,
            $"Completed delta page processing: pageCount={pageCount}, totalItems={totalItemsProcessed}",
            cancellationToken);

        return (finalDelta, pageCount, totalItemsProcessed);
    }
}
