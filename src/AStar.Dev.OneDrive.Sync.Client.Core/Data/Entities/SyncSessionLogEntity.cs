using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;

/// <summary>
///     Database entity for sync session log.
/// </summary>
public class SyncSessionLogEntity
{
    public Guid Id { get; set; }= Guid.CreateVersion7();
    public HashedAccountId HashedAccountId { get; set; }
    public DateTimeOffset StartedUtc { get; set; }
    public DateTimeOffset? CompletedUtc { get; set; }
    public int Status { get; set; }
    public int FilesUploaded { get; set; }
    public int FilesDownloaded { get; set; }
    public int FilesDeleted { get; set; }
    public int ConflictsDetected { get; set; }
    public long TotalBytes { get; set; }
}
