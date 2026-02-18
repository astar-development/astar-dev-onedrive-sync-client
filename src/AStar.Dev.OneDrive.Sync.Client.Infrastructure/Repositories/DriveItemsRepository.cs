using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;

/// <summary>
///     Repository implementation for managing file metadata.
/// </summary>
public sealed class DriveItemsRepository(IDbContextFactory<SyncDbContext> contextFactory) : IDriveItemsRepository
{
    private readonly IDbContextFactory<SyncDbContext> _contextFactory = contextFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileMetadata>> GetByAccountIdAsync(HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DriveItemEntity> entities = await context.DriveItems
            .AsNoTracking()
            .Where(fm => fm.HashedAccountId == hashedAccountId)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc />
    public async Task<FileMetadata?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DriveItemEntity? entity = await context.DriveItems.FindAsync([id], cancellationToken);
        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task<FileMetadata?> GetByPathAsync(HashedAccountId hashedAccountId, string path, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DriveItemEntity? entity = await context.DriveItems
            .FirstOrDefaultAsync(driveItem => driveItem.HashedAccountId == hashedAccountId && driveItem.RelativePath == path, cancellationToken);

        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task AddAsync(FileMetadata fileMetadata, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DriveItemEntity driveItem = MapToEntity(fileMetadata);
        if(context.DriveItems.Any(driveItem => driveItem.DriveItemId == fileMetadata.DriveItemId))
        {
            await UpdateAsync(fileMetadata, cancellationToken);
            return;
        }

        _ = context.DriveItems.Add(driveItem);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(FileMetadata fileMetadata, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DriveItemEntity driveItem = await context.DriveItems.FindAsync([fileMetadata.DriveItemId], cancellationToken) ??
                                    throw new InvalidOperationException($"File metadata with ID '{fileMetadata.DriveItemId}' not found.");
        DriveItemEntity updatedEntity = MapToEntity(fileMetadata);

        context.Entry(driveItem).CurrentValues.SetValues(updatedEntity);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DriveItemEntity? entity = await context.DriveItems.FindAsync([id], cancellationToken);
        if(entity is not null)
        {
            _ = context.DriveItems.Remove(entity);
            _ = await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task SaveBatchAsync(IEnumerable<FileMetadata> fileMetadataList, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        var entities = fileMetadataList.Select(MapToEntity).ToList();

        // Does this run during the delta sync? If so, we need to be careful to handle additions, updates, and deletions correctly. For now, we'll just upsert all items.
        foreach(DriveItemEntity? driveItem in entities)
        {
            DriveItemEntity? existing = await context.DriveItems.FindAsync([driveItem.DriveItemId], cancellationToken);
            if(existing is null)
                _ = context.DriveItems.Add(driveItem);
            else
                context.Entry(existing).CurrentValues.SetValues(driveItem);
        }

        _ = await context.SaveChangesAsync(cancellationToken);
    }

    private static FileMetadata MapToModel(DriveItemEntity driveItem) => new(
                driveItem.DriveItemId,
                driveItem.HashedAccountId,
                driveItem.Name ?? string.Empty,
                driveItem.RelativePath,
                driveItem.Size,
                driveItem.LastModifiedUtc,
                driveItem.LocalPath ?? string.Empty,
                driveItem.IsFolder,
                driveItem.IsDeleted,
                driveItem.IsSelected ?? false,
                driveItem.RemoteHash,
                driveItem.CTag,
                driveItem.ETag,
                driveItem.LocalHash,
                driveItem.SyncStatus,
                driveItem.LastSyncDirection
            );

    private static DriveItemEntity MapToEntity(FileMetadata fileMetadata) => new(fileMetadata.HashedAccountId, fileMetadata.DriveItemId, fileMetadata.RelativePath, fileMetadata.ETag, fileMetadata.CTag, fileMetadata.Size, fileMetadata.LastModifiedUtc, fileMetadata.IsFolder, fileMetadata.IsDeleted, fileMetadata.IsSelected, fileMetadata.RemoteHash, fileMetadata.Name, fileMetadata.LocalPath, fileMetadata.LocalHash, fileMetadata.SyncStatus, fileMetadata.LastSyncDirection ?? SyncDirection.None);
}
