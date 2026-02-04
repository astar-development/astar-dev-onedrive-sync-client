namespace AStar.Dev.OneDrive.Sync.Client.Common.Models;

/// <summary>
/// AccountSettings value object (record) for account configuration.
/// Immutable value object suitable for DTOs and settings transfer.
/// </summary>
/// <param name="HomeSyncDirectory">Directory for syncing files. Can be null or empty.</param>
/// <param name="MaxConcurrent">Maximum concurrent operations. Default: 5.</param>
/// <param name="DebugLoggingEnabled">Enable verbose debug logging for this account.</param>
public record AccountSettings(string? HomeSyncDirectory, int MaxConcurrent, bool DebugLoggingEnabled);
