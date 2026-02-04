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
    /// Gets or sets the hashed account ID associated with this conflict.
    /// GDPR compliant: stores hashed ID, not actual account ID.
    /// </summary>
    public string HashedAccountId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the file system item ID involved in the conflict.
    /// </summary>
    public string ItemId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the local file path involved in the conflict.
    /// </summary>
    public string? LocalPath { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the local file was last modified.
    /// </summary>
    public DateTime? LocalLastModified { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the remote file was last modified.
    /// </summary>
    public DateTime? RemoteLastModified { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the conflict was resolved.
    /// </summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Gets or sets the resolution action taken: 'keep_local', 'keep_remote', 'both', 'ignore'.
    /// </summary>
    public ResolutionAction ResolutionAction { get; set; }

    /// <summary>
    /// Gets or sets the type of conflict: 'none', 'local_newer', 'remote_newer', 'both_modified'.
    /// </summary>
    public ConflictType ConflictType { get; set; }
}
