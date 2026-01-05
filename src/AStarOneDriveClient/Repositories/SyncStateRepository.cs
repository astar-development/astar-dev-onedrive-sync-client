using AStarOneDriveClient.Data;
using AStarOneDriveClient.Data.Entities;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AStarOneDriveClient.Repositories;

/// <summary>
/// Repository implementation for managing sync state data.
/// </summary>
public sealed class SyncStateRepository : ISyncStateRepository
{
    private readonly SyncDbContext _context;

    public SyncStateRepository(SyncDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<SyncState?> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        var entity = await _context.SyncStates.FindAsync([accountId], cancellationToken);
        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncState>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.SyncStates.ToListAsync(cancellationToken);
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc/>
    public async Task SaveAsync(SyncState syncState, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(syncState);

        var entity = await _context.SyncStates.FindAsync([syncState.AccountId], cancellationToken);

        if (entity is null)
        {
            entity = MapToEntity(syncState);
            _context.SyncStates.Add(entity);
        }
        else
        {
            entity.Status = (int)syncState.Status;
            entity.TotalFiles = syncState.TotalFiles;
            entity.CompletedFiles = syncState.CompletedFiles;
            entity.TotalBytes = syncState.TotalBytes;
            entity.CompletedBytes = syncState.CompletedBytes;
            entity.FilesDownloading = syncState.FilesDownloading;
            entity.FilesUploading = syncState.FilesUploading;
            entity.ConflictsDetected = syncState.ConflictsDetected;
            entity.MegabytesPerSecond = syncState.MegabytesPerSecond;
            entity.EstimatedSecondsRemaining = syncState.EstimatedSecondsRemaining;
            entity.LastUpdateUtc = syncState.LastUpdateUtc;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        var entity = await _context.SyncStates.FindAsync([accountId], cancellationToken);
        if (entity is not null)
        {
            _context.SyncStates.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static SyncState MapToModel(SyncStateEntity entity) =>
        new(
            entity.AccountId,
            (SyncStatus)entity.Status,
            entity.TotalFiles,
            entity.CompletedFiles,
            entity.TotalBytes,
            entity.CompletedBytes,
            entity.FilesDownloading,
            entity.FilesUploading,
            entity.ConflictsDetected,
            entity.MegabytesPerSecond,
            entity.EstimatedSecondsRemaining,
            entity.LastUpdateUtc
        );

    private static SyncStateEntity MapToEntity(SyncState model) =>
        new()
        {
            AccountId = model.AccountId,
            Status = (int)model.Status,
            TotalFiles = model.TotalFiles,
            CompletedFiles = model.CompletedFiles,
            TotalBytes = model.TotalBytes,
            CompletedBytes = model.CompletedBytes,
            FilesDownloading = model.FilesDownloading,
            FilesUploading = model.FilesUploading,
            ConflictsDetected = model.ConflictsDetected,
            MegabytesPerSecond = model.MegabytesPerSecond,
            EstimatedSecondsRemaining = model.EstimatedSecondsRemaining,
            LastUpdateUtc = model.LastUpdateUtc
        };
}
