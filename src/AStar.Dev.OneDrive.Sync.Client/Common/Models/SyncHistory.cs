namespace AStar.Dev.OneDrive.Sync.Client.Common.Models;

/// <summary>
/// Represents a sync operation audit trail entry.
/// </summary>
public class SyncHistory
{
    /// <summary>
    /// Gets or sets the unique identifier for the sync history entry.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the account ID associated with this sync operation.
    /// </summary>
    public string AccountId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of sync: 'manual', 'scheduled', 'background'.
    /// </summary>
    public string? SyncType { get; set; }

    /// <summary>
    /// Gets or sets the sync direction: 'upload', 'download', 'bidirectional'.
    /// </summary>
    public string? SyncDirection { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the sync operation started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the sync operation completed.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the status of the sync operation: 'success', 'partial', 'failed'.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Gets or sets the number of items uploaded during this sync.
    /// </summary>
    public int? ItemsUploaded { get; set; }

    /// <summary>
    /// Gets or sets the number of items downloaded during this sync.
    /// </summary>
    public int? ItemsDownloaded { get; set; }

    /// <summary>
    /// Gets or sets the error message if the sync failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Navigation property to the associated account.
    /// </summary>
    public Account Account { get; set; } = null!;
}