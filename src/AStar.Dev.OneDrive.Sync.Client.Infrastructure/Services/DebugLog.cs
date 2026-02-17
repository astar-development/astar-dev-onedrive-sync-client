using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Static facade for convenient debug logging access throughout the application.
///     Provides static methods that delegate to the singleton IDebugLogger instance.
/// </summary>
public static class DebugLog
{
    private static IDebugLogger? _instance;
    private static IDebugLogRepository? _debugRepository;
    private static IAccountRepository? _accountRepository;

    /// <summary>
    ///     Initializes the static logger instance. Should be called once during application startup.
    /// </summary>
    /// <param name="logger">The IDebugLogger instance to use.</param>
    public static void Initialize(IDebugLogger logger) => _instance = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    ///    Initializes the static logger instance with the necessary repositories. Should be called once during application startup before any logging methods are used.
    /// </summary>
    /// <param name="repository">The debug log repository instance to use.</param>
    /// <param name="accountRepository">The account repository instance to use.</param>
    public static void Initialize(IDebugLogRepository repository, IAccountRepository accountRepository) => (_debugRepository, _accountRepository) = (repository, accountRepository);

    /// <summary>
    ///     Logs an informational message.
    /// </summary>
    /// <param name="source">The source of the log (typically class or method name).</param>
    /// <param name="message">The log message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task InfoAsync(string source, HashedAccountId hashedAccountId, string message, CancellationToken cancellationToken = default) => _instance?.LogInfoAsync(source, hashedAccountId, message, cancellationToken) ?? Task.CompletedTask;

    /// <summary>
    ///     Logs an informational message - if debug logging is enabled.
    /// </summary>
    /// <param name="source">The source of the log (typically class or method name).</param>
    /// <param name="message">The log message.</param>
    /// <param name="hashedAccountId">The hashed account ID associated with the log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Task<Result<Unit, ErrorResponse>> LogInfoAsync(string source, HashedAccountId hashedAccountId, string message, CancellationToken cancellationToken = default)
        => LogMessage(source, hashedAccountId, message, LogLevel.Information, null, cancellationToken);

    /// <summary>
    ///    Logs an error message.
    /// </summary>
    /// <param name="source">The source of the log (typically class or method name).</param>
    /// <param name="hashedAccountId">The hashed account ID associated with the log.</param>
    /// <param name="message">The log message.</param>
    /// <param name="exception">Optional exception details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    public static Task<Result<Unit, ErrorResponse>> LogErrorAsync(string source, HashedAccountId hashedAccountId, string message, Exception? exception = null, CancellationToken cancellationToken = default)
        => LogMessage(source, hashedAccountId, message, LogLevel.Error, exception, cancellationToken);

    /// <summary>
    ///     Logs an error message.
    /// </summary>
    /// <param name="source">The source of the log (typically class or method name).</param>
    /// <param name="message">The log message.</param>
    /// <param name="exception">Optional exception details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task ErrorAsync(string source, HashedAccountId hashedAccountId, string message, Exception? exception = null, CancellationToken cancellationToken = default)
        => _instance?.LogErrorAsync(source, hashedAccountId, message, exception, cancellationToken) ?? Task.CompletedTask;

    /// <summary>
    ///     Logs a method entry.
    /// </summary>
    /// <param name="source">The source of the log (typically class.Method).</param>
    /// <param name="hashedAccountId">The hashed account ID associated with the log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task EntryAsync(string source, HashedAccountId hashedAccountId, CancellationToken cancellationToken = default) => _instance?.LogEntryAsync(source, hashedAccountId, cancellationToken) ?? Task.CompletedTask;

    /// <summary>
    ///     Logs a method exit.
    /// </summary>
    /// <param name="source">The source of the log (typically class.Method).</param>
    /// <param name="hashedAccountId">The hashed account ID associated with the log.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public static Task ExitAsync(string source, HashedAccountId hashedAccountId, CancellationToken cancellationToken = default) => _instance?.LogExitAsync(source, hashedAccountId, cancellationToken) ?? Task.CompletedTask;

    private static async Task<Result<Unit, ErrorResponse>> LogMessage(string source, HashedAccountId hashedAccountId, string message, LogLevel logLevel, Exception? exception = null, CancellationToken cancellationToken = default)
        => await Try.RunAsync<Result<Unit, ErrorResponse>>(async () =>
            {
                AccountInfo? account = await _accountRepository!.GetByIdAsync(hashedAccountId, cancellationToken);

                if(account is null)
                    return new ErrorResponse($"Account with ID {hashedAccountId} not found");

                if(account.EnableDebugLogging)
                    await _debugRepository!.AddAsync(DebugLogEntry.Create(hashedAccountId, source, message, logLevel.ToString(), exception), cancellationToken);

                return Unit.Value;
            })
            .MatchAsync(
                success => success,
                ex => new Result<Unit, ErrorResponse>.Error(new ErrorResponse($"Failed to log debug message: {ex.Message}"))
            );
}
