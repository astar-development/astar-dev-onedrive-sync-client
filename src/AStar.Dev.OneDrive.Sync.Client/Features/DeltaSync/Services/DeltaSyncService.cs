using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.GraphApi;
using AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Repositories;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using DeltaChangeType = AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Models.ChangeType;
using DeltaChangeModel = AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Models.DeltaChange;

namespace AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Services;

/// <summary>
/// Service for detecting remote changes using Microsoft Graph API delta queries.
/// </summary>
public class DeltaSyncService(GraphApiClientFactory graphFactory, IDeltaTokenRepository deltaTokenRepository) : IDeltaSyncService
{
    private readonly GraphApiClientFactory _graphFactory = graphFactory ?? throw new ArgumentNullException(nameof(graphFactory));
    private readonly IDeltaTokenRepository _deltaTokenRepository = deltaTokenRepository ?? throw new ArgumentNullException(nameof(deltaTokenRepository));

    /// <inheritdoc />
    public async Task<DeltaSyncResult> GetDeltaChangesAsync(string accessToken, string hashedAccountId, string driveName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashedAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(driveName);

        GraphServiceClient client = _graphFactory.CreateClient(accessToken);

        DeltaToken? savedToken = await _deltaTokenRepository.GetByAccountAndDriveAsync(hashedAccountId, driveName);
        
        // Note: This is a simplified implementation.
        // Full delta query implementation with delta tokens requires additional Microsoft Graph SDK setup.
        // For now, we'll return an empty result to allow the structure to compile.
        // TODO: Implement actual delta query when Graph SDK delta support is properly configured.
        
        return new DeltaSyncResult
        {
            Changes = Array.Empty<DeltaChangeModel>(),
            DeltaToken = savedToken?.Token
        };
    }

    private static DeltaChangeModel ParseDriveItem(DriveItem item)
    {
        DeltaChangeType changeType = item.Deleted is not null
            ? DeltaChangeType.Deleted
            : item.File is not null || item.Folder is not null
                ? DeltaChangeType.Modified
                : DeltaChangeType.Added;

        return new DeltaChangeModel
        {
            DriveItemId = item.Id ?? string.Empty,
            Name = item.Name ?? string.Empty,
            Path = item.ParentReference?.Path,
            IsFolder = item.Folder is not null,
            ChangeType = changeType,
            RemoteModifiedAt = item.LastModifiedDateTime?.DateTime,
            RemoteHash = item.File?.Hashes?.QuickXorHash
        };
    }
}
