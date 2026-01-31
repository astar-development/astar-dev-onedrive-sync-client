using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

/// <summary>
///     Repository implementation for managing file operation logs.
/// </summary>
public sealed class FileOperationLogRepository(SyncDbContext context) : IFileOperationLogRepository
{
    private readonly SyncDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileOperationLog>> GetBySessionIdAsync(string syncSessionId, CancellationToken cancellationToken = default)
    {
        List<FileOperationLogEntity> entities = await _context.FileOperationLogs
            .AsNoTracking()
            .Where(f => f.SyncSessionId == syncSessionId)
            .OrderBy(f => f.Timestamp)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileOperationLog>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        List<FileOperationLogEntity> entities = await _context.FileOperationLogs
            .AsNoTracking()
            .Where(f => f.AccountId == accountId)
            .OrderByDescending(f => f.Timestamp)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileOperationLog>> GetByAccountIdAsync(string accountId, int pageSize, int skip, CancellationToken cancellationToken = default)
    {
        List<FileOperationLogEntity> entities = await _context.FileOperationLogs
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
        FileOperationLogEntity entity = MapToEntity(operationLog);
        _ = _context.FileOperationLogs.Add(entity);
        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteOldOperationsAsync(string accountId, DateTimeOffset olderThan, CancellationToken cancellationToken = default)
        => await _context.FileOperationLogs
            .Where(f => f.AccountId == accountId && f.Timestamp < olderThan)
            .ExecuteDeleteAsync(cancellationToken);

    private static FileOperationLog MapToModel(FileOperationLogEntity entity)
        => new(
            entity.Id,
            entity.SyncSessionId,
            entity.AccountId,
            entity.Timestamp,
            (FileOperation)entity.Operation,
            entity.FilePath,
            entity.LocalPath,
            entity.OneDriveId,
            entity.FileSize,
            entity.LocalHash,
            entity.RemoteHash,
            entity.LastModifiedUtc,
            entity.Reason);

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
