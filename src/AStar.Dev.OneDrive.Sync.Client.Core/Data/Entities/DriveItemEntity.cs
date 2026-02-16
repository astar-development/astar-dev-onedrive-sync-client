using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;

public sealed class DriveItemEntity(
    HashedAccountId hashedAccountId,
    string driveItemId,
    string relativePath,
    string? eTag,
    string? cTag,
    long size,
    DateTimeOffset lastModifiedUtc,
    bool isFolder,
    bool isDeleted,
    bool? isSelected = false,
    string? remoteHash = null,
    string? name = null,
    string? localPath = null,
    string? localHash = null,
    FileSyncStatus syncStatus = FileSyncStatus.SyncOnly,
    SyncDirection lastSyncDirection = SyncDirection.None)
{
    public HashedAccountId HashedAccountId { get; set; } = hashedAccountId;
    public string DriveItemId { get; set; } = driveItemId;
    public string RelativePath { get; set; } = relativePath;
    public string? ETag { get; set; } = eTag;
    public string? CTag { get; set; } = cTag;
    public long Size { get; set; } = size;
    public DateTimeOffset LastModifiedUtc { get; set; } = lastModifiedUtc;
    public bool IsFolder { get; set; } = isFolder;
    public bool IsDeleted { get; set; } = isDeleted;
    public bool? IsSelected { get; set; } = isSelected;
    public string? RemoteHash { get; set; } = remoteHash;
    public string? Name { get; set; } = name;
    public string? LocalPath { get; set; } = localPath;
    public string? LocalHash { get; set; } = localHash;
    public FileSyncStatus SyncStatus { get; set; } = syncStatus;
    public SyncDirection LastSyncDirection { get; set; } = lastSyncDirection;

    public DriveItemEntity WithUpdatedSelection(bool isSelected) => new(
                HashedAccountId,
                DriveItemId,
                RelativePath,
                ETag,
                CTag,
                Size,
                LastModifiedUtc,
                IsFolder,
                IsDeleted,
                isSelected,
                RemoteHash,
                Name,
                LocalPath,
                LocalHash,
                SyncStatus,
                LastSyncDirection
            );

    public DriveItemEntity WithUpdatedDetails(bool isSelected, string relativePath, DateTimeOffset lastModifiedUtc) => new(
                HashedAccountId,
                DriveItemId,
                relativePath,
                ETag,
                CTag,
                Size,
                lastModifiedUtc,
                IsFolder,
                IsDeleted,
                isSelected,
                RemoteHash,
                Name,
                LocalPath,
                LocalHash,
                SyncStatus,
                LastSyncDirection
            );
}
