namespace AStar.Dev.OneDrive.Sync.Client.Common.Models;

/// <summary>
/// Represents per-account diagnostic and logging settings.
/// </summary>
public class DiagnosticSettings
{
    /// <summary>
    /// Gets or sets the unique identifier for the diagnostic settings.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Gets or sets the hashed account ID associated with these diagnostic settings.
    /// This should be unique per account.
    /// GDPR compliant: stores hashed ID, not actual account ID.
    /// </summary>
    public string HashedAccountId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the log level: 'Trace', 'Debug', 'Information', 'Warning', 'Error', 'Critical'.
    /// </summary>
    public ApplicationLogLevel LogLevel { get; set; } = ApplicationLogLevel.Information;

    /// <summary>
    /// Gets or sets a value indicating whether diagnostic logging is enabled for this account.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the timestamp when these diagnostic settings were created.
    /// </summary>
    public DateTime? CreatedAt { get; set; }
}
