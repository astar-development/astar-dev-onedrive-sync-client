using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;

namespace AStarOneDriveClient.Services.Sync;

/// <summary>
///     Service for resolving sync conflicts between local and remote files.
/// </summary>
public interface IConflictResolver
{
    /// <summary>
    ///     Applies the user's chosen resolution strategy to a conflict.
    /// </summary>
    /// <param name="conflict">The conflict to resolve.</param>
    /// <param name="strategy">The resolution strategy chosen by the user.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ResolveAsync(
        SyncConflict conflict,
        ConflictResolutionStrategy strategy,
        CancellationToken cancellationToken = default);
}
