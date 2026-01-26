using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

/// <summary>
///     Repository implementation for managing sync configuration data.
/// </summary>
public sealed class SyncConfigurationRepository(IDbContextFactory<SyncDbContext> contextFactory) : ISyncConfigurationRepository
{
    private readonly IDbContextFactory<SyncDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileMetadata>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DriveItemEntity> entities = await context.DriveItems
            .Where(sc => sc.AccountId == accountId)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetSelectedFoldersAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        return await context.DriveItems
                .Where(sc => sc.AccountId == accountId && sc.IsSelected)
                .Select(sc => CleanUpPath(sc.RelativePath))
                .Distinct()
                .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<IList<string>, ErrorResponse>> GetSelectedFolders2Async(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        return await context.DriveItems
                .Where(sc => sc.AccountId == accountId && sc.IsSelected)
                .Select(sc => CleanUpPath(sc.RelativePath))
                .Distinct()
                .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileMetadata> AddAsync(FileMetadata configuration, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DriveItemEntity? existingEntity = await context.DriveItems
            .FirstOrDefaultAsync(sc => sc.AccountId == configuration.AccountId && sc.RelativePath == configuration.RelativePath, cancellationToken);

        if(existingEntity is not null)
            return configuration;

        DriveItemEntity entity = MapToEntity(configuration);
        _ = context.DriveItems.Add(entity);
        _ = await context.SaveChangesAsync(cancellationToken);

        return configuration;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(FileMetadata configuration, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DriveItemEntity syncConfiguration = await context.DriveItems.FindAsync([configuration.Id], cancellationToken) ??
                                         throw new InvalidOperationException($"Sync configuration with ID '{configuration.Id}' not found.");

        DriveItemEntity syncConfiguration1 = syncConfiguration.WithUpdatedDetails(configuration.IsSelected, configuration.RelativePath, configuration.LastModifiedUtc);

        context.Entry(syncConfiguration1).CurrentValues.SetValues(syncConfiguration1);

        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
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
    public async Task DeleteByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DriveItemEntity> entities = await context.DriveItems
            .Where(sc => sc.AccountId == accountId)
            .ToListAsync(cancellationToken);

        context.DriveItems.RemoveRange(entities);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveBatchAsync(string accountId, IEnumerable<FileMetadata> configurations, CancellationToken cancellationToken = default)
    {
        var configDict = configurations.ToDictionary(c => c.Id);

        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DriveItemEntity> existingEntities = await context.DriveItems
            .Where(sc => sc.AccountId == accountId && sc.IsFolder)
            .ToListAsync(cancellationToken);

        foreach(DriveItemEntity entity in existingEntities)
        {
            var isSelected = configDict.TryGetValue(entity.Id, out FileMetadata? config)
                ? config.IsSelected
                : entity.IsSelected;

            if(entity.IsSelected != isSelected)
            {
                DriveItemEntity updatedEntity = entity.WithUpdatedSelection(isSelected);
                context.Entry(entity).CurrentValues.SetValues(updatedEntity);
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DriveItemEntity?> GetParentFolderAsync(string accountId, string parentPath, string possibleParentPath, CancellationToken cancellationToken)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DriveItemEntity? parentEntity = await context.DriveItems
            .FirstOrDefaultAsync(sc => sc.AccountId == accountId && (sc.RelativePath == parentPath || sc.RelativePath == possibleParentPath), cancellationToken);
        return parentEntity;
    }

    private static string CleanUpPath(string localFolderPath)
    {
        var indexOfDrives = localFolderPath.IndexOf("drives", StringComparison.OrdinalIgnoreCase);
        if(indexOfDrives >= 0)
        {
            var indexOfColon = localFolderPath.IndexOf(":/", StringComparison.OrdinalIgnoreCase);
            if(indexOfColon > 0)
            {
                var part1 = localFolderPath[..indexOfDrives];
                var part2 = localFolderPath[(indexOfColon + 2)..];
                localFolderPath = part1 + part2;
            }
        }

        return localFolderPath;
    }

    private static FileMetadata MapToModel(DriveItemEntity driveItemEntity)
        => new(
            driveItemEntity.Id,
            driveItemEntity.AccountId,
            driveItemEntity.Name ?? string.Empty,
             driveItemEntity.DriveItemId,
             driveItemEntity.RelativePath,
            driveItemEntity.Size,
            driveItemEntity.LastModifiedUtc,
             driveItemEntity.LocalPath ?? string.Empty,
            driveItemEntity.IsFolder,
            driveItemEntity.IsDeleted,
            driveItemEntity.IsSelected,
             driveItemEntity.RelativePath ?? string.Empty,
            driveItemEntity.ETag,
            driveItemEntity.CTag
        );

    private static DriveItemEntity MapToEntity(FileMetadata model)
        => new(
            model.AccountId,
            model.Id,
            model.DriveItemId,
            model.RelativePath,
            model.ETag,
            model.CTag,
            model.Size,
            model.LastModifiedUtc,
            model.IsFolder,
            model.IsDeleted,
            model.IsSelected
        );
}
