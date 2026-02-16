using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models;

/// <summary>
///     Represents a file synchronization conflict.
/// </summary>
/// <param name="Id">Unique identifier for the conflict.</param>
/// <param name="AccountId">The account ID this conflict belongs to.</param>
/// <param name="HashedAccountId">Hashed account identifier for privacy.</param>
/// <param name="FilePath">Path to the conflicted file.</param>
/// <param name="LocalModifiedUtc">Local file modification timestamp.</param>
/// <param name="RemoteModifiedUtc">OneDrive file modification timestamp.</param>
/// <param name="LocalSize">Local file size in bytes.</param>
/// <param name="RemoteSize">OneDrive file size in bytes.</param>
/// <param name="DetectedUtc">Timestamp when conflict was detected.</param>
/// <param name="ResolutionStrategy">Strategy chosen to resolve the conflict.</param>
/// <param name="IsResolved">Indicates whether the conflict has been resolved.</param>
public sealed record SyncConflict(
    string Id,
    string AccountId,
    HashedAccountId HashedAccountId,
    string FilePath,
    DateTimeOffset LocalModifiedUtc,
    DateTimeOffset RemoteModifiedUtc,
    long LocalSize,
    long RemoteSize,
    DateTimeOffset DetectedUtc,
    ConflictResolutionStrategy ResolutionStrategy,
    bool IsResolved
)
{
    public static SyncConflict CreateUnresolvedConflict(string accountId,HashedAccountId hashedAccountId, string filePath, DateTimeOffset localModifiedUtc, DateTimeOffset remoteModifiedUtc, long localSize, long remoteSize) => new(
        Guid.CreateVersion7().ToString(),
        accountId,
        hashedAccountId,
        filePath,
        localModifiedUtc,
        remoteModifiedUtc,
        localSize,
        remoteSize,
        DateTime.UtcNow,
        ConflictResolutionStrategy.None,
        false);
}
