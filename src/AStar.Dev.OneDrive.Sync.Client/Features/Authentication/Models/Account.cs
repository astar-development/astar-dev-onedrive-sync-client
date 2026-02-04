namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;

/// <summary>
/// Account entity representing a OneDrive account with secure token storage and sync settings.
/// Hashed identifiers are used to comply with GDPR privacy requirements.
/// </summary>
public class Account
{

    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// SHA256 hash of the user's email (case-insensitive).
    /// </summary>
    public string HashedEmail
    {
        get;
        set
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("HashedEmail cannot be null or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;

    /// <summary>
    /// SHA256 hash of the account ID with salt (createdAtTicks).
    /// </summary>
    public string HashedAccountId
    {
        get;
        set
        {
            if(string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("HashedAccountId cannot be null or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;

    /// <summary>
    /// User-configured directory for syncing files.
    /// Can be empty/null if not yet configured.
    /// </summary>
    public string? HomeSyncDirectory { get; set; }

    /// <summary>
    /// Maximum concurrent operations (uploads/downloads).
    /// Default: 5. Range: 1-unlimited (validated at service level).
    /// </summary>
    public int MaxConcurrent { get; set; } = 5;

    /// <summary>
    /// Enable verbose debug logging for this account.
    /// </summary>
    public bool DebugLoggingEnabled { get; set; }

    /// <summary>
    /// Administrative flag for special account privileges.
    /// </summary>
    public bool IsAdmin { get; set; }

    /// <summary>
    /// Account creation timestamp (UTC).
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp (UTC).
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property: FileSystemItems (folders selected for sync).
    /// </summary>
    public ICollection<object> FileSystemItems { get; } = [];

    /// <summary>
    /// Navigation property: DeltaTokens (one per OneDrive drive).
    /// </summary>
    public ICollection<object> DeltaTokens { get; } = [];

    /// <summary>
    /// Navigation property: ConflictLogs (sync conflict history).
    /// </summary>
    public ICollection<object> ConflictLogs { get; } = [];

    /// <summary>
    /// Navigation property: ApplicationLogs (per-account debug logs).
    /// </summary>
    public ICollection<object> ApplicationLogs { get; } = [];
}
