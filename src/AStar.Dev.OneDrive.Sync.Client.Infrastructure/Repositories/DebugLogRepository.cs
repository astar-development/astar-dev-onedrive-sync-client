using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;

/// <summary>
///     Repository implementation for accessing debug log entries.
/// </summary>
public sealed class DebugLogRepository(IDbContextFactory<SyncDbContext> contextFactory) : IDebugLogRepository
{
    private readonly IDbContextFactory<SyncDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

    /// <inheritdoc />
    public async Task<IReadOnlyList<DebugLogEntry>> GetByAccountIdAsync(string accountId, int pageSize, int skip, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DebugLogEntity> entities = await context.DebugLogs
            .Where(log => log.HashedAccountId == accountId)
            .OrderByDescending(log => log.TimestampUtc)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return
        [
            .. entities.Select(debugLog => new DebugLogEntry(
                debugLog.Id,
                debugLog.HashedAccountId,
                debugLog.TimestampUtc,
                debugLog.LogLevel,
                debugLog.Source,
                debugLog.Message,
                debugLog.Exception
            ))
        ];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DebugLogEntry>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DebugLogEntity> entities = await context.DebugLogs
            .Where(log => log.HashedAccountId == accountId)
            .OrderByDescending(log => log.TimestampUtc)
            .ToListAsync(cancellationToken);

        return
        [
            .. entities.Select(debugLog => new DebugLogEntry(
                debugLog.Id,
                debugLog.HashedAccountId,
                debugLog.TimestampUtc,
                debugLog.LogLevel,
                debugLog.Source,
                debugLog.Message,
                debugLog.Exception
            ))
        ];
    }

    /// <inheritdoc />
    public async Task DeleteByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DebugLogEntity> entities = await context.DebugLogs
            .Where(log => log.HashedAccountId == accountId)
            .ToListAsync(cancellationToken);

        context.DebugLogs.RemoveRange(entities);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteOlderThanAsync(DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DebugLogEntity> entities = await context.DebugLogs
            .Where(log => log.TimestampUtc < olderThan)
            .ToListAsync(cancellationToken);

        context.DebugLogs.RemoveRange(entities);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetDebugLogCountByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        return await context.DebugLogs
                .Where(log => log.HashedAccountId == accountId)
                .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(DebugLogEntry debugLogEntry, CancellationToken cancellationToken)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DebugLogEntity entity = MapToEntity(debugLogEntry);
        _ = await context.DebugLogs.AddAsync(entity, cancellationToken);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    private static DebugLogEntity MapToEntity(DebugLogEntry debugLogEntry)
        => new()
        {
            Id = debugLogEntry.Id,
            HashedAccountId = debugLogEntry.AccountId,
            TimestampUtc = debugLogEntry.Timestamp,
            LogLevel = debugLogEntry.LogLevel,
            Source = debugLogEntry.Source,
            Message = debugLogEntry.Message,
            Exception = debugLogEntry.Exception
        };
}
