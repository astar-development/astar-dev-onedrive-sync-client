using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.Source.Generators.Attributes;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

/// <summary>
///     Implementation of debug logging that writes to the database when enabled for an account.
///     Seems the AutoRegisterService still isn't working correctly but, as it doesn't break anything, I am leaving here for now
/// </summary>
[AutoRegisterService()]
public sealed class DebugLoggerService(IDbContextFactory<SyncDbContext> contextFactory, IAccountRepository accountRepository) : IDebugLogger
{
    /// <inheritdoc />
    public async Task LogInfoAsync(string source, string accountId, string message, CancellationToken cancellationToken = default) => await LogAsync("Info", source, message, null, accountId, cancellationToken);

    /// <inheritdoc />
    public async Task LogErrorAsync(string source, string accountId, string message, Exception? exception = null, CancellationToken cancellationToken = default)
        => await LogAsync("Error", source, message, exception, accountId, cancellationToken);
    /// <inheritdoc />
    public async Task LogEntryAsync(string source, string accountId, CancellationToken cancellationToken = default) => await LogAsync("Entry", source, "Method entry", null, accountId, cancellationToken);

    /// <inheritdoc />
    public async Task LogExitAsync(string source, string accountId, CancellationToken cancellationToken = default) => await LogAsync("Exit", source, "Method exit", null, accountId, cancellationToken);

    private async Task LogAsync(string logLevel, string source, string message, Exception? exception, string accountId, CancellationToken cancellationToken)
    {
        if(string.IsNullOrEmpty(accountId))
            return;

        AccountInfo? account = await accountRepository.GetByIdAsync(accountId, cancellationToken);
        if(account is null || !account.EnableDebugLogging)
            return;

        string? exceptionString = null;
        if(exception is not null)
        {
            exceptionString = exception.ToString();
            if(exceptionString.Length > 4000)
                exceptionString = exceptionString[..4000] + " [truncated]";
        }

        // Create a fresh DbContext instance isolated from other operations
        await using SyncDbContext context = contextFactory.CreateDbContext();
        var logEntry = new DebugLogEntity
        {
            AccountId = accountId,
            TimestampUtc = DateTimeOffset.UtcNow,
            LogLevel = logLevel,
            Source = source,
            Message = message,
            Exception = exceptionString
        };

        _ = context.DebugLogs.Add(logEntry);
        _ = await context.SaveChangesAsync(cancellationToken);
    }
}
