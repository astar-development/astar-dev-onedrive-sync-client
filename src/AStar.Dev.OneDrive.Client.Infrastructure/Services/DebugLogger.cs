using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

/// <summary>
///     Implementation of debug logging that writes to the database when enabled for an account.
///     Seems the AutoRegisterService still isn't working correctly but, as it doesn't break anything, I am leaving here for now 
/// </summary>
[AutoRegisterService(ServiceLifetime.Scoped)]
public sealed class DebugLogger(SyncDbContext context, IAccountRepository accountRepository) : IDebugLogger
{
    /// <inheritdoc />
    public async Task LogInfoAsync(string source, string message, CancellationToken cancellationToken = default) => await LogAsync("Info", source, message, null, cancellationToken);

    /// <inheritdoc />
    public async Task LogErrorAsync(string source, string message, Exception? exception = null, CancellationToken cancellationToken = default)
        => await LogAsync("Error", source, message, exception, cancellationToken);

    /// <inheritdoc />
    public async Task LogEntryAsync(string source, CancellationToken cancellationToken = default) => await LogAsync("Entry", source, "Method entry", null, cancellationToken);

    /// <inheritdoc />
    public async Task LogExitAsync(string source, CancellationToken cancellationToken = default) => await LogAsync("Exit", source, "Method exit", null, cancellationToken);

    private async Task LogAsync(string logLevel, string source, string message, Exception? exception, CancellationToken cancellationToken)
    {
        var accountId = DebugLogContext.CurrentAccountId;

        // Check if debug logging is enabled for this account
        AccountInfo? account = await accountRepository.GetByIdAsync(accountId, cancellationToken);
        if(account is null || !account.EnableDebugLogging) return; // Debug logging not enabled for this account

        var logEntry = new DebugLogEntity
        {
            AccountId = accountId,
            TimestampUtc = DateTime.UtcNow,
            LogLevel = logLevel,
            Source = source,
            Message = message,
            Exception = exception?.ToString()
        };

        _ = context.DebugLogs.Add(logEntry);
        _ = await context.SaveChangesAsync(cancellationToken);
    }
}
