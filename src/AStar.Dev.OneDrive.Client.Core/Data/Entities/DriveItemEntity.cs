using AStar.Dev.OneDrive.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Client.Core.Data.Entities;

public record DriveItemEntity(
    string AccountId,
    string Id,
    string DriveItemId,
    string RelativePath,
    string? ETag,
    string? CTag,
    long Size,
    DateTimeOffset LastModifiedUtc,
    bool IsFolder,
    bool IsDeleted,
    string? Name = null,
    string? LocalPath = null,
    string? LocalHash = null,
    FileSyncStatus SyncStatus = FileSyncStatus.SyncOnly,
    SyncDirection LastSyncDirection = SyncDirection.None);
