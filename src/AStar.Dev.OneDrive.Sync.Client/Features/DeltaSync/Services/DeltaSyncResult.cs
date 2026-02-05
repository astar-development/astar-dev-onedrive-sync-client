using AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Features.DeltaSync.Services;

/// <summary>
/// Result of a delta sync operation.
/// </summary>
public class DeltaSyncResult
{
    /// <summary>
    /// Gets or sets the detected changes.
    /// </summary>
    public required IReadOnlyList<DeltaChange> Changes { get; init; }

    /// <summary>
    /// Gets or sets the new delta token to save for the next sync.
    /// </summary>
    public required string? DeltaToken { get; init; }
}
