using AStar.Dev.OneDrive.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Client.Core.Models;

/// <summary>
///     Represents metadata for a synchronized file.
/// </summary>
/// <param name="Id">Unique identifier (typically OneDrive item ID).</param>
/// <param name="AccountId">Account identifier.</param>
/// <param name="Name">File name.</param>
/// <param name="RelativePath">OneDrive path to the file.</param>
/// <param name="Size">File size in bytes.</param>
/// <param name="LastModifiedUtc">Last modification timestamp.</param>
/// <param name="LocalPath">Local file system path.</param>
/// <param name="CTag">OneDrive cTag for change tracking.</param>
/// <param name="ETag">OneDrive eTag for versioning.</param>
/// <param name="LocalHash">SHA256 hash of local file content.</param>
/// <param name="SyncStatus">Current synchronization status of the file.</param>
/// <param name="LastSyncDirection">Direction of last synchronization operation.</param>
public sealed record FileMetadata(
    string Id,
    string AccountId,
    string Name,
    string DriveItemId,
    string RelativePath,
    long Size,
    DateTimeOffset LastModifiedUtc,
    string LocalPath,
    bool IsFolder = false,
    bool IsDeleted = false,
    bool IsSelected = false,
    string? RemoteHash = null,
    string? CTag = null,
    string? ETag = null,
    string? LocalHash = null,
    FileSyncStatus SyncStatus = FileSyncStatus.SyncOnly,
    SyncDirection? LastSyncDirection = SyncDirection.None
);
