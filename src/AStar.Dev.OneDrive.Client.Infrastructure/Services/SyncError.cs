namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

/// <summary>
///     Represents errors that can occur during sync operations.
/// </summary>
public sealed record SyncError(string Message, Exception? Exception = null)
{
    /// <summary>
    ///     Creates a SyncError for when account validation fails.
    /// </summary>
    public static SyncError AccountNotFound(string accountId) => new($"Account '{accountId}' not found");

    /// <summary>
    ///     Creates a SyncError for delta processing failures.
    /// </summary>
    public static SyncError DeltaProcessingFailed(string message, Exception? exception = null) => new($"Delta processing failed: {message}", exception);

    /// <summary>
    ///     Creates a SyncError for general sync failures.
    /// </summary>
    public static SyncError SyncFailed(string message, Exception? exception = null) => new($"Sync failed: {message}", exception);
}
