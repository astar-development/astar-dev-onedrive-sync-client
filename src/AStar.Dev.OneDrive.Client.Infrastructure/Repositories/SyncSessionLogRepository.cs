using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

/// <summary>
///     Repository implementation for managing sync session logs.
/// </summary>
public sealed class SyncSessionLogRepository(SyncDbContext context) : ISyncSessionLogRepository
{
    private readonly SyncDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncSessionLog>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        List<SyncSessionLogEntity> entities = await _context.SyncSessionLogs
            .AsNoTracking()
            .Where(s => s.AccountId == accountId)
            .OrderByDescending(s => s.StartedUtc)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc />
    public async Task<SyncSessionLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        SyncSessionLogEntity? entity = await _context.SyncSessionLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return entity is not null ? MapToModel(entity) : null;
    }

    /// <inheritdoc />
    public async Task AddAsync(SyncSessionLog sessionLog, CancellationToken cancellationToken = default)
    {
        SyncSessionLogEntity entity = MapToEntity(sessionLog);
        _ = _context.SyncSessionLogs.Add(entity);
        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(SyncSessionLog sessionLog, CancellationToken cancellationToken = default)
    {
        SyncSessionLogEntity syncSessionLog = await _context.SyncSessionLogs.FindAsync([sessionLog.Id], cancellationToken) ??
                                      throw new InvalidOperationException($"Sync session log with ID '{sessionLog.Id}' not found.");

        syncSessionLog.CompletedUtc = sessionLog.CompletedUtc;
        syncSessionLog.Status = (int)sessionLog.Status;
        syncSessionLog.FilesUploaded = sessionLog.FilesUploaded;
        syncSessionLog.FilesDownloaded = sessionLog.FilesDownloaded;
        syncSessionLog.FilesDeleted = sessionLog.FilesDeleted;
        syncSessionLog.ConflictsDetected = sessionLog.ConflictsDetected;
        syncSessionLog.TotalBytes = sessionLog.TotalBytes;

        _ = await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteOldSessionsAsync(string accountId, DateTimeOffset olderThan, CancellationToken cancellationToken = default) => _ = await _context.SyncSessionLogs
        .Where(s => s.AccountId == accountId && s.StartedUtc < olderThan)
        .ExecuteDeleteAsync(cancellationToken);

    private static SyncSessionLog MapToModel(SyncSessionLogEntity syncSessionLog)
        => new(
            syncSessionLog.Id,
            syncSessionLog.AccountId,
            syncSessionLog.StartedUtc,
            syncSessionLog.CompletedUtc,
            (SyncStatus)syncSessionLog.Status,
            syncSessionLog.FilesUploaded,
            syncSessionLog.FilesDownloaded,
            syncSessionLog.FilesDeleted,
            syncSessionLog.ConflictsDetected,
            syncSessionLog.TotalBytes);

    private static SyncSessionLogEntity MapToEntity(SyncSessionLog model)
        => new()
        {
            Id = model.Id,
            AccountId = model.AccountId,
            StartedUtc = model.StartedUtc,
            CompletedUtc = model.CompletedUtc,
            Status = (int)model.Status,
            FilesUploaded = model.FilesUploaded,
            FilesDownloaded = model.FilesDownloaded,
            FilesDeleted = model.FilesDeleted,
            ConflictsDetected = model.ConflictsDetected,
            TotalBytes = model.TotalBytes
        };
}
