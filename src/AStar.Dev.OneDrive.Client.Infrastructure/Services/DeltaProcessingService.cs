using AStar.Dev.OneDrive.Client.Core;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

/// <summary>
///     Service for managing delta token storage and processing delta queries from OneDrive.
/// </summary>
public sealed class DeltaProcessingService : IDeltaProcessingService
{
    private readonly ISyncRepository _syncRepository;
    private readonly IGraphApiClient _graphApiClient;
    private readonly IDeltaPageProcessor _deltaPageProcessor;

    public DeltaProcessingService(
        ISyncRepository syncRepository,
        IGraphApiClient graphApiClient,
        IDeltaPageProcessor deltaPageProcessor)
    {
        _syncRepository = syncRepository ?? throw new ArgumentNullException(nameof(syncRepository));
        _graphApiClient = graphApiClient ?? throw new ArgumentNullException(nameof(graphApiClient));
        _deltaPageProcessor = deltaPageProcessor ?? throw new ArgumentNullException(nameof(deltaPageProcessor));
    }

    /// <inheritdoc />
    public async Task<DeltaToken?> GetDeltaTokenAsync(string accountId, CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync(DebugLogMetadata.Services.DeltaProcessingService.GetDeltaTokenAsync, accountId, cancellationToken);
        
        DeltaToken? token = await _syncRepository.GetDeltaTokenAsync(accountId, cancellationToken);
        
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
        
        await _syncRepository.SaveOrUpdateDeltaTokenAsync(token, cancellationToken);
        
        await DebugLog.InfoAsync(
            DebugLogMetadata.Services.DeltaProcessingService.SaveDeltaTokenAsync,
            token.AccountId,
            $"Saved delta token for account {token.AccountId}",
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(DeltaToken finalToken, int pageCount, int totalItemsProcessed)> ProcessDeltaPagesAsync(
        string accountId,
        DeltaToken? deltaToken,
        Action<SyncState>? progressCallback,
        CancellationToken cancellationToken)
    {
        await DebugLog.EntryAsync(DebugLogMetadata.Services.DeltaProcessingService.ProcessDeltaPagesAsync, accountId, cancellationToken);
        
        // If no delta token provided, create an empty one to start from the beginning
        DeltaToken tokenToUse = deltaToken ?? new DeltaToken(accountId, string.Empty, string.Empty, DateTimeOffset.UtcNow);
        
        await DebugLog.InfoAsync(
            DebugLogMetadata.Services.DeltaProcessingService.ProcessDeltaPagesAsync,
            accountId,
            $"Starting delta page processing for account {accountId}, initialSync={string.IsNullOrEmpty(tokenToUse.Token)}",
            cancellationToken);
        
        (DeltaToken? finalDelta, int pageCount, int totalItemsProcessed) = 
            await _deltaPageProcessor.ProcessAllDeltaPagesAsync(
                accountId,
                tokenToUse,
                progressCallback,
                cancellationToken);
        
        await DebugLog.InfoAsync(
            DebugLogMetadata.Services.DeltaProcessingService.ProcessDeltaPagesAsync,
            accountId,
            $"Completed delta page processing: pageCount={pageCount}, totalItems={totalItemsProcessed}",
            cancellationToken);
        
        return (finalDelta, pageCount, totalItemsProcessed);
    }
}
