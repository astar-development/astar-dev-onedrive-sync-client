using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

/// <summary>
///     Repository implementation for managing file operation logs.
/// </summary>
public sealed class FileOperationLogRepository(IDbContextFactory<SyncDbContext> contextFactory) : IFileOperationLogRepository
{
    private readonly IDbContextFactory<SyncDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileOperationLog>> GetBySessionIdAsync(string syncSessionId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<FileOperationLogEntity> entities = await context.FileOperationLogs
            .AsNoTracking()
            .Where(f => f.SyncSessionId == syncSessionId)
            .OrderBy(f => f.Timestamp)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileOperationLog>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<FileOperationLogEntity> entities = await context.FileOperationLogs
            .AsNoTracking()
            .Where(f => f.AccountId == accountId)
            .OrderByDescending(f => f.Timestamp)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileOperationLog>> GetByAccountIdAsync(string accountId, int pageSize, int skip, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<FileOperationLogEntity> entities = await context.FileOperationLogs
            .AsNoTracking()
            .Where(f => f.AccountId == accountId)
            .OrderByDescending(f => f.Timestamp)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc />
    public async Task AddAsync(FileOperationLog operationLog, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        FileOperationLogEntity entity = MapToEntity(operationLog);
        _ = context.FileOperationLogs.Add(entity);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteOldOperationsAsync(string accountId, DateTimeOffset olderThan, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        _ = await context.FileOperationLogs
                .Where(f => f.AccountId == accountId && f.Timestamp < olderThan)
                .ExecuteDeleteAsync(cancellationToken);
    }

    private static FileOperationLog MapToModel(FileOperationLogEntity fileOperationLog)
        => new(
            fileOperationLog.Id,
            fileOperationLog.SyncSessionId,
            fileOperationLog.AccountId,
            fileOperationLog.Timestamp,
            (FileOperation)fileOperationLog.Operation,
            fileOperationLog.FilePath,
            fileOperationLog.LocalPath,
            fileOperationLog.OneDriveId,
            fileOperationLog.FileSize,
            fileOperationLog.LocalHash,
            fileOperationLog.RemoteHash,
            fileOperationLog.LastModifiedUtc,
            fileOperationLog.Reason);

    private static FileOperationLogEntity MapToEntity(FileOperationLog model)
        => new()
        {
            Id = model.Id,
            SyncSessionId = model.SyncSessionId,
            AccountId = model.AccountId,
            Timestamp = model.Timestamp,
            Operation = (int)model.Operation,
            FilePath = model.FilePath,
            LocalPath = model.LocalPath,
            OneDriveId = model.OneDriveId,
            FileSize = model.FileSize,
            LocalHash = model.LocalHash,
            RemoteHash = model.RemoteHash,
            LastModifiedUtc = model.LastModifiedUtc,
            Reason = model.Reason
        };
}
