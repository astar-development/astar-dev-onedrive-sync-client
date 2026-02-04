namespace AStar.Dev.OneDrive.Sync.Client.Common.Models;

/// <summary>
/// Represents a delta token for incremental sync operations.
/// </summary>
public class DeltaToken
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed account identifier this token belongs to.
    /// GDPR compliant: stores hashed ID, not actual account ID.
    /// </summary>
    public string HashedAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the drive name (e.g., "root", "documents", "photos").
    /// </summary>
    public string DriveName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the opaque delta token from Graph API.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the last successful sync timestamp.
    /// </summary>
    public DateTime? LastSyncAt { get; set; }

}
