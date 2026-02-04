namespace AStar.Dev.OneDrive.Sync.Client.Common.Models;

/// <summary>
/// Represents a OneDrive account in the sync client.
/// Uses hashed identifiers for GDPR compliance.
/// </summary>
public class Account
{
    /// <summary>
    /// Gets or sets the unique identifier. SHA256 hash of email + timestamp.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the hashed email for lookups. SHA256(email.ToLower()).
    /// </summary>
    public string HashedEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name (user-provided or nickname).
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the token storage key reference.
    /// </summary>
    public string? TokenStorageKey { get; set; }

    /// <summary>
    /// Gets or sets the user-configurable local sync directory path.
    /// </summary>
    public string? HomeSyncDirectory { get; set; }

    /// <summary>
    /// Gets or sets the maximum concurrent downloads limit.
    /// </summary>
    public int MaxConcurrentDownloads { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum concurrent uploads limit.
    /// </summary>
    public int MaxConcurrentUploads { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether debug logging is enabled for this account.
    /// </summary>
    public bool EnableDebugLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the account creation timestamp.
    /// </summary>
    public DateTime? CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last authentication refresh timestamp.
    /// </summary>
    public DateTime? LastAuthRefresh { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the account is active.
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the account is an administrator.
    /// </summary>
    public bool? IsAdmin { get; set; }
}
