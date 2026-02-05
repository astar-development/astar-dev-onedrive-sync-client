namespace AStar.Dev.OneDrive.Sync.Client.Common.Models;

/// <summary>
/// Represents a delta token for incremental sync operations.
/// </summary>
public class DeltaToken
{
    private string _id = string.Empty;
    private string _hashedAccountId = string.Empty;
    private string _driveName = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public string Id
    {
        get => _id;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Id cannot be null, empty, or whitespace.", nameof(Id));
            }
            _id = value;
        }
    }

    /// <summary>
    /// Gets or sets the hashed account identifier this token belongs to.
    /// GDPR compliant: stores hashed ID, not actual account ID.
    /// </summary>
    public string HashedAccountId
    {
        get => _hashedAccountId;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("HashedAccountId cannot be null, empty, or whitespace.", nameof(HashedAccountId));
            }
            _hashedAccountId = value;
        }
    }

    /// <summary>
    /// Gets or sets the drive name (e.g., "root", "documents", "photos").
    /// </summary>
    public string DriveName
    {
        get => _driveName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("DriveName cannot be null, empty, or whitespace.", nameof(DriveName));
            }
            _driveName = value;
        }
    }

    /// <summary>
    /// Gets or sets the opaque delta token from Graph API.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Gets or sets the last successful sync timestamp.
    /// </summary>
    public DateTime? LastSyncAt { get; set; }

}
