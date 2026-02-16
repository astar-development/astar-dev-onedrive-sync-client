using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;

/// <summary>
///     Database entity for file operation log.
/// </summary>
public class FileOperationLogEntity
{
    public string Id { get; set; } = string.Empty;
    public string SyncSessionId { get; set; } = string.Empty;
    public HashedAccountId HashedAccountId { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public int Operation { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public string? OneDriveId { get; set; }
    public long FileSize { get; set; }
    public string? LocalHash { get; set; }
    public string? RemoteHash { get; set; }
    public DateTimeOffset LastModifiedUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}
