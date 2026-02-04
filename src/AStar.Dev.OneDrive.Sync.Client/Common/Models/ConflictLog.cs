namespace AStar.Dev.OneDrive.Sync.Client.Common.Models;

/// <summary>
/// Represents a sync conflict that requires user resolution.
/// </summary>
public class ConflictLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the conflict log.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the account ID associated with this conflict.
    /// </summary>
    public string AccountId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the file system item ID involved in the conflict.
    /// </summary>
    public string ItemId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the local file path involved in the conflict.
    /// </summary>
    public string? LocalPath { get; set; }

    /// <summary>
    /// Gets or sets the type of conflict: 'local_newer', 'remote_newer', 'both_modified'.
    /// </summary>
    public string? ConflictType { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the local file was last modified.
    /// </summary>
    public DateTime? LocalLastModified { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the remote file was last modified.
    /// </summary>
    public DateTime? RemoteLastModified { get; set; }

    /// <summary>
    /// Gets or sets the resolution action taken: 'keep_local', 'keep_remote', 'both', 'ignore'.
    /// </summary>
    public string? ResolutionAction { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the conflict was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Navigation property to the associated account.
    /// </summary>
    public Account Account { get; set; } = null!;
}