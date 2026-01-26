using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

/// <summary>
///     Repository implementation for managing file metadata.
/// </summary>
public sealed class DriveItemsRepository(IDbContextFactory<SyncDbContext> contextFactory) : IDriveItemsRepository
{
    private readonly IDbContextFactory<SyncDbContext> _contextFactory = contextFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileMetadata>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DriveItemEntity> entities = await context.DriveItems
            .AsNoTracking()
            .Where(fm => fm.AccountId == accountId)
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
    public async Task<FileMetadata?> GetByPathAsync(string accountId, string path, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DriveItemEntity? entity = await context.DriveItems
            .FirstOrDefaultAsync(driveItem => driveItem.AccountId == accountId && driveItem.RelativePath == path, cancellationToken);

        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task AddAsync(FileMetadata fileMetadata, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DriveItemEntity driveItem = MapToEntity(fileMetadata);
        if(context.DriveItems.Any(driveItem => driveItem.Id == driveItem.Id))
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
        DriveItemEntity driveItem = await context.DriveItems.FindAsync([fileMetadata.Id], cancellationToken) ??
                                    throw new InvalidOperationException($"File metadata with ID '{fileMetadata.Id}' not found.");

        context.Entry(driveItem).CurrentValues.SetValues(driveItem);
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

        foreach(DriveItemEntity? driveItem in entities)
        {
            DriveItemEntity? existing = await context.DriveItems.FindAsync([driveItem.Id], cancellationToken);
            if(existing is null)
                _ = context.DriveItems.Add(driveItem);
            else
                context.Entry(existing).CurrentValues.SetValues(driveItem);
        }

        _ = await context.SaveChangesAsync(cancellationToken);
    }

    private static FileMetadata MapToModel(DriveItemEntity driveItem)
        => new(
            driveItem.Id,
            driveItem.AccountId,
            driveItem.Name ?? string.Empty,
            driveItem.DriveItemId,
            driveItem.RelativePath,
            driveItem.Size,
            driveItem.LastModifiedUtc,
            driveItem.LocalPath ?? string.Empty,
            driveItem.IsFolder,
            driveItem.IsDeleted,
            driveItem.IsSelected,
            driveItem.RemoteHash,
            driveItem.CTag,
            driveItem.ETag,
            driveItem.LocalHash,
            (FileSyncStatus)driveItem.SyncStatus,
            driveItem.LastSyncDirection
        );

    private static DriveItemEntity MapToEntity(FileMetadata fileMetadata)
        => new(fileMetadata.AccountId, fileMetadata.Id, fileMetadata.Id, fileMetadata.RelativePath, fileMetadata.ETag, fileMetadata.CTag, fileMetadata.Size, fileMetadata.LastModifiedUtc, fileMetadata.IsFolder, fileMetadata.IsDeleted, fileMetadata.IsSelected, fileMetadata.RemoteHash, fileMetadata.Name, fileMetadata.LocalPath, fileMetadata.LocalHash, fileMetadata.SyncStatus, fileMetadata.LastSyncDirection ?? SyncDirection.None);
}
