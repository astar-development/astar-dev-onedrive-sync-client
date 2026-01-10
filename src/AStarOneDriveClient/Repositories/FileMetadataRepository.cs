using AStarOneDriveClient.Data;
using AStarOneDriveClient.Data.Entities;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace AStarOneDriveClient.Repositories;

/// <summary>
/// Repository implementation for managing file metadata.
/// </summary>
public sealed class FileMetadataRepository : IFileMetadataRepository
{
    private readonly SyncDbContext _context;

    public FileMetadataRepository(SyncDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FileMetadata>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        var entities = await _context.FileMetadata
            .AsNoTracking()
            .Where(fm => fm.AccountId == accountId)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc/>
    public async Task<FileMetadata?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        var entity = await _context.FileMetadata.FindAsync([id], cancellationToken);
        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc/>
    public async Task<FileMetadata?> GetByPathAsync(string accountId, string path, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(path);

        var entity = await _context.FileMetadata
            .FirstOrDefaultAsync(fm => fm.AccountId == accountId && fm.Path == path, cancellationToken);

        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<FileMetadata>> GetByStatusAsync(string accountId, FileSyncStatus status, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        var entities = await _context.FileMetadata
            .Where(fm => fm.AccountId == accountId && fm.SyncStatus == (int)status)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc/>
    public async Task AddAsync(FileMetadata fileMetadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileMetadata);

        var entity = MapToEntity(fileMetadata);
        if (_context.FileMetadata.Any(fm => fm.Id == entity.Id))
        {
            await UpdateAsync(fileMetadata, cancellationToken);
            return;
        }

        _context.FileMetadata.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(FileMetadata fileMetadata, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileMetadata);

        var entity = await _context.FileMetadata.FindAsync([fileMetadata.Id], cancellationToken) ?? throw new InvalidOperationException($"File metadata with ID '{fileMetadata.Id}' not found.");

        entity.AccountId = fileMetadata.AccountId;
        entity.Name = fileMetadata.Name;
        entity.Path = fileMetadata.Path;
        entity.Size = fileMetadata.Size;
        entity.LastModifiedUtc = fileMetadata.LastModifiedUtc;
        entity.LocalPath = fileMetadata.LocalPath;
        entity.CTag = fileMetadata.CTag;
        entity.ETag = fileMetadata.ETag;
        entity.LocalHash = fileMetadata.LocalHash;
        entity.SyncStatus = (int)fileMetadata.SyncStatus;
        entity.LastSyncDirection = fileMetadata.LastSyncDirection.HasValue ? (int)fileMetadata.LastSyncDirection.Value : null;

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);

        var entity = await _context.FileMetadata.FindAsync([id], cancellationToken);
        if (entity is not null)
        {
            _context.FileMetadata.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        var entities = await _context.FileMetadata
            .Where(fm => fm.AccountId == accountId)
            .ToListAsync(cancellationToken);

        _context.FileMetadata.RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SaveBatchAsync(IEnumerable<FileMetadata> fileMetadataList, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileMetadataList);

        var entities = fileMetadataList.Select(MapToEntity).ToList();

        foreach (var entity in entities)
        {
            var existing = await _context.FileMetadata.FindAsync([entity.Id], cancellationToken);
            if (existing is null)
            {
                _context.FileMetadata.Add(entity);
            }
            else
            {
                _context.Entry(existing).CurrentValues.SetValues(entity);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static FileMetadata MapToModel(FileMetadataEntity entity) =>
        new(
            entity.Id,
            entity.AccountId,
            entity.Name,
            entity.Path,
            entity.Size,
            entity.LastModifiedUtc,
            entity.LocalPath,
            entity.CTag,
            entity.ETag,
            entity.LocalHash,
            (FileSyncStatus)entity.SyncStatus,
            entity.LastSyncDirection.HasValue ? (SyncDirection)entity.LastSyncDirection.Value : null
        );

    private static FileMetadataEntity MapToEntity(FileMetadata model) =>
        new()
        {
            Id = model.Id,
            AccountId = model.AccountId,
            Name = model.Name,
            Path = model.Path,
            Size = model.Size,
            LastModifiedUtc = model.LastModifiedUtc,
            LocalPath = model.LocalPath,
            CTag = model.CTag,
            ETag = model.ETag,
            LocalHash = model.LocalHash,
            SyncStatus = (int)model.SyncStatus,
            LastSyncDirection = model.LastSyncDirection.HasValue ? (int)model.LastSyncDirection.Value : null
        };
}
