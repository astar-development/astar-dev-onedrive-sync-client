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
    public async Task<IReadOnlyList<DriveItemEntity>> GetSelectedItemsByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        return await context.DriveItems
                .Where(sc => sc.AccountId == accountId && (sc.IsSelected ?? false))
                .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetSelectedFoldersAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        return await context.DriveItems
                .Where(sc => sc.AccountId == accountId && (sc.IsSelected ?? false))
                .Select(sc => CleanUpPath(sc.RelativePath))
                .Distinct()
                .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DriveItemEntity>> GetFoldersByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        return await context.DriveItems
                .Where(sc => sc.AccountId == accountId && sc.IsFolder)
                .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<bool, ErrorResponse>> UpdateFoldersByAccountIdAsync(string accountId, IEnumerable<FileMetadata> fileMetadatas, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        var existingItems = context.DriveItems
            .Where(driveItem => driveItem.AccountId == accountId).ToList();

        var metaLookup = fileMetadatas
            .ToDictionary(
                m => Normalize(m.RelativePath),
                m => m.IsSelected
            );

        existingItems.ApplyHierarchicalSelection(fileMetadatas);

        return await context.SaveChangesAsync(cancellationToken) > 0
            ? true
            : new ErrorResponse("No changes were made to the selected folders.");
    }

    /// <inheritdoc />
    public async Task<FileMetadata> AddAsync(FileMetadata configuration, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DriveItemEntity? existingEntity = await context.DriveItems
            .FirstOrDefaultAsync(sc => sc.AccountId == configuration.AccountId && sc.DriveItemId == configuration.DriveItemId, cancellationToken);

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
        DriveItemEntity syncConfiguration = await context.DriveItems.FindAsync([configuration.DriveItemId], cancellationToken) ??
                                         throw new InvalidOperationException($"Sync configuration with ID '{configuration.DriveItemId}' not found.");

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
        var configDict = configurations.ToDictionary(c => c.DriveItemId);

        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DriveItemEntity> existingEntities = await context.DriveItems
            .Where(sc => sc.AccountId == accountId && sc.IsFolder)
            .ToListAsync(cancellationToken);

        foreach(DriveItemEntity entity in existingEntities)
        {
            var isSelected = configDict.TryGetValue(entity.DriveItemId, out FileMetadata? config)
                ? config.IsSelected
                : entity.IsSelected;

            if(entity.IsSelected != isSelected)
            {
                DriveItemEntity updatedEntity = entity.WithUpdatedSelection(isSelected??false);
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
            driveItemEntity.DriveItemId,
            driveItemEntity.AccountId,
            driveItemEntity.Name ?? string.Empty,
             driveItemEntity.RelativePath,
            driveItemEntity.Size,
            driveItemEntity.LastModifiedUtc,
             driveItemEntity.LocalPath ?? string.Empty,
            driveItemEntity.IsFolder,
            driveItemEntity.IsDeleted,
            driveItemEntity.IsSelected ?? false,
             driveItemEntity.RelativePath ?? string.Empty,
            driveItemEntity.ETag,
            driveItemEntity.CTag
        );

    private static DriveItemEntity MapToEntity(FileMetadata model)
        => new(
            model.AccountId,
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

    private static string Normalize(string path)
    {
        if(string.IsNullOrWhiteSpace(path))
            return "/";

        path = path.Trim().Replace("\\", "/").TrimEnd('/');

        if(!path.StartsWith('/'))
            path = "/" + path;

        return path;
    }
}
