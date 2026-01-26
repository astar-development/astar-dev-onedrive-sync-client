namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

/// <summary>
///     Service for logging debug information to the database when enabled for an account.
/// </summary>
public interface IDebugLogger
{
    /// <summary>
    ///     Logs an informational message.
    /// </summary>
    /// <param name="source">The source of the log (typically class or method name).</param>
    /// <param name="accountId">The account ID associated with the log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogInfoAsync(string source, string accountId, string message, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Logs an error message.
    /// </summary>
    /// <param name="source">The source of the log (typically class or method name).</param>
    /// <param name="accountId">The account ID associated with the log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="exception">Optional exception details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogErrorAsync(string source, string accountId, string message, Exception? exception = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    ///     Logs a method entry.
    /// </summary>
    /// <param name="source">The source of the log (typically class.Method).</param>
    /// <param name="accountId">The account ID associated with the log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogEntryAsync(string source, string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Logs a method exit.
    /// </summary>
    /// <param name="source">The source of the log (typically class.Method).</param>
    /// <param name="accountId">The account ID associated with the log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogExitAsync(string source, string accountId, CancellationToken cancellationToken = default);
}
