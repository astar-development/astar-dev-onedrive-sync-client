using AStar.Dev.OneDrive.Sync.Client.Core.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;

/// <summary>
///     Entity for storing debug log entries in the database.
/// </summary>
public sealed class DebugLogEntity
{
    public int Id { get; set; }
    public required HashedAccountId HashedAccountId { get; set; }
    public DateTimeOffset TimestampUtc { get; set; }
    public required string LogLevel { get; set; }
    public required string Source { get; set; }
    public required string Message { get; set; }
    public string? Exception { get; set; }
}
