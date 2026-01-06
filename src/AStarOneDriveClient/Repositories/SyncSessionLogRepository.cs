using AStarOneDriveClient.Data;
using AStarOneDriveClient.Data.Entities;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AStarOneDriveClient.Repositories;

/// <summary>
/// Repository implementation for managing sync session logs.
/// </summary>
public sealed class SyncSessionLogRepository : ISyncSessionLogRepository
{
    private readonly SyncDbContext _context;

    public SyncSessionLogRepository(SyncDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncSessionLog>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        var entities = await _context.SyncSessionLogs
            .AsNoTracking()
            .Where(s => s.AccountId == accountId)
            .OrderByDescending(s => s.StartedUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc/>
    public async Task<SyncSessionLog?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        var entity = await _context.SyncSessionLogs
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return entity is not null ? MapToModel(entity) : null;
    }

    /// <inheritdoc/>
    public async Task AddAsync(SyncSessionLog sessionLog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionLog);

        var entity = MapToEntity(sessionLog);
        _context.SyncSessionLogs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(SyncSessionLog sessionLog, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sessionLog);

        var entity = await _context.SyncSessionLogs.FindAsync([sessionLog.Id], cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException($"Sync session log with ID '{sessionLog.Id}' not found.");
        }

        entity.CompletedUtc = sessionLog.CompletedUtc;
        entity.Status = (int)sessionLog.Status;
        entity.FilesUploaded = sessionLog.FilesUploaded;
        entity.FilesDownloaded = sessionLog.FilesDownloaded;
        entity.FilesDeleted = sessionLog.FilesDeleted;
        entity.ConflictsDetected = sessionLog.ConflictsDetected;
        entity.TotalBytes = sessionLog.TotalBytes;

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteOldSessionsAsync(string accountId, DateTime olderThan, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        await _context.SyncSessionLogs
            .Where(s => s.AccountId == accountId && s.StartedUtc < olderThan)
            .ExecuteDeleteAsync(cancellationToken);
    }

    private static SyncSessionLog MapToModel(SyncSessionLogEntity entity) =>
        new(
            entity.Id,
            entity.AccountId,
            entity.StartedUtc,
            entity.CompletedUtc,
            (SyncStatus)entity.Status,
            entity.FilesUploaded,
            entity.FilesDownloaded,
            entity.FilesDeleted,
            entity.ConflictsDetected,
            entity.TotalBytes);

    private static SyncSessionLogEntity MapToEntity(SyncSessionLog model) =>
        new()
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
