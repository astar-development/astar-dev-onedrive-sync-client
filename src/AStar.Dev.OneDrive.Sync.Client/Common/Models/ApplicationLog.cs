namespace AStar.Dev.OneDrive.Sync.Client.Common.Models;

/// <summary>
/// Represents a structured application log entry for the log viewer.
/// </summary>
public class ApplicationLog
{
    /// <summary>
    /// Gets or sets the unique identifier for the log entry.
    /// Uses auto-incrementing BigInt (BIGSERIAL in PostgreSQL).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the account ID associated with this log entry.
    /// NULL for global logs not associated with a specific account.
    /// </summary>
    public string? AccountId { get; set; }

    /// <summary>
    /// Gets or sets the log level: 'Trace', 'Debug', 'Information', 'Warning', 'Error', 'Critical'.
    /// </summary>
    public string LogLevel { get; set; } = null!;

    /// <summary>
    /// Gets or sets the timestamp when the log entry was created.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the log message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the exception details if an exception was logged.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Gets or sets the source context (logger name/category).
    /// </summary>
    public string? SourceContext { get; set; }

    /// <summary>
    /// Gets or sets the structured log properties as JSONB.
    /// </summary>
    public string? Properties { get; set; }

    /// <summary>
    /// Navigation property to the associated account.
    /// NULL for global logs.
    /// </summary>
    public Account? Account { get; set; }
}