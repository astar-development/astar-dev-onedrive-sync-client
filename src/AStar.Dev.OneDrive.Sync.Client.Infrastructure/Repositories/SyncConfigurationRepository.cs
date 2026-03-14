using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;

/// <summary>
///     Repository implementation for managing sync configuration data.
/// </summary>
public sealed class SyncConfigurationRepository(IDbContextFactory<SyncDbContext> contextFactory) : ISyncConfigurationRepository
{
    private readonly IDbContextFactory<SyncDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

    /// <inheritdoc />
    public async Task<IReadOnlyList<FileMetadata>> GetByAccountIdAsync(HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DriveItemEntity> entities = await context.DriveItems
            .Where(sc => sc.HashedAccountId == hashedAccountId)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DriveItemEntity>> GetSelectedItemsByAccountIdAsync(HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        return await context.DriveItems
                .Where(sc => sc.HashedAccountId == hashedAccountId && (sc.IsSelected ?? false))
                .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetSelectedFoldersAsync(HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        return await context.DriveItems
                .Where(sc => sc.HashedAccountId == hashedAccountId && (sc.IsSelected ?? false))
                .Select(sc => CleanUpPath(sc.RelativePath))
                .Distinct()
                .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DriveItemEntity>> GetFoldersByAccountIdAsync(HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        return await context.DriveItems
                .Where(sc => sc.HashedAccountId == hashedAccountId && sc.IsFolder)
                .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DriveItemEntity>> GetAllSelectedItemsByAccountIdAsync(HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DriveItemEntity> f = await context.DriveItems.FromSqlRaw(@"SELECT * FROM DriveItems
WHERE RelativePath IN (
    SELECT RelativePath
    FROM DriveItems
    WHERE IsSelected = 1 AND HashedAccountId = {0}
) AND HashedAccountId = {0}", hashedAccountId.Value).ToListAsync(cancellationToken);

return f;
    }

    /// <inheritdoc />
    public async Task<FileMetadata> AddAsync(FileMetadata configuration, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DriveItemEntity? existingEntity = await context.DriveItems
            .FirstOrDefaultAsync(sc => sc.HashedAccountId == configuration.HashedAccountId && sc.DriveItemId == configuration.DriveItemId, cancellationToken);

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
    public async Task DeleteByAccountIdAsync(HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DriveItemEntity> entities = await context.DriveItems
            .Where(sc => sc.HashedAccountId == hashedAccountId)
            .ToListAsync(cancellationToken);

        context.DriveItems.RemoveRange(entities);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveBatchAsync(HashedAccountId hashedAccountId, IEnumerable<FileMetadata> configurations, CancellationToken cancellationToken = default)
    {
        var configDict = configurations.ToDictionary(c => c.DriveItemId);

        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<DriveItemEntity> existingEntities = await context.DriveItems
            .Where(sc => sc.HashedAccountId == hashedAccountId && sc.IsFolder)
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
    public async Task<DriveItemEntity?> GetParentFolderAsync(HashedAccountId hashedAccountId, string parentPath, string possibleParentPath, CancellationToken cancellationToken)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        DriveItemEntity? parentEntity = await context.DriveItems
            .FirstOrDefaultAsync(sc => sc.HashedAccountId == hashedAccountId && (sc.RelativePath == parentPath || sc.RelativePath == possibleParentPath), cancellationToken);
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

    private static FileMetadata MapToModel(DriveItemEntity driveItemEntity) => new(
                driveItemEntity.DriveItemId,
                driveItemEntity.HashedAccountId,
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

    private static DriveItemEntity MapToEntity(FileMetadata model) => new(
                model.HashedAccountId,
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

    /// <inheritdoc />
    public async Task UpdateFoldersByAccountIdAsync(HashedAccountId hashedAccountId, List<OneDriveFolderNode> rootFolders, CancellationToken cancellationToken)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();

        var allFolders = new List<OneDriveFolderNode>();
        foreach(OneDriveFolderNode root in rootFolders)
        {
            allFolders.Add(root);
            allFolders.AddRange(await AddChildFoldersRecursivelyAsync(hashedAccountId, root.Children.ToList(), cancellationToken));
        }   

        foreach(OneDriveFolderNode folderNode in allFolders)
        {
            DriveItemEntity? existingEntity = await context.DriveItems
                .FirstOrDefaultAsync(e => e.HashedAccountId == hashedAccountId && e.DriveItemId == folderNode.DriveItemId, cancellationToken);

            if(existingEntity is not null   )
            {
                ApplyNodeValues(existingEntity, folderNode);
                context.Entry(existingEntity).CurrentValues.SetValues(existingEntity);
            }
            else
            {
                DriveItemEntity newEntity = CreateEntityFromNode(hashedAccountId, folderNode);
                _ = context.DriveItems.Add(newEntity);
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<OneDriveFolderNode>> AddChildFoldersRecursivelyAsync(HashedAccountId hashedAccountId, List<OneDriveFolderNode> parentFolders, CancellationToken cancellationToken)
    {
        var allFolders = new List<OneDriveFolderNode>();

        foreach(OneDriveFolderNode parent in parentFolders.Where(p => p.IsFolder))
        {
            allFolders.Add(parent);
            allFolders.AddRange(await AddChildFoldersRecursivelyAsync(hashedAccountId, parent.Children.Where(d=>d.IsFolder).ToList(), cancellationToken));
        }

        return allFolders;
    }

    private static void ApplyNodeValues(DriveItemEntity entity, OneDriveFolderNode node)
    {
        entity.Name = node.Name;
        entity.RelativePath = node.Path;
        entity.IsFolder = node.IsFolder;
        entity.IsSelected = node.IsSelected;
    }

    private static DriveItemEntity CreateEntityFromNode(HashedAccountId hashedAccountId, OneDriveFolderNode node)
        => new(hashedAccountId, node.DriveItemId, node.Path, null, null, 0, DateTimeOffset.UtcNow, node.IsFolder, false, node.IsSelected, name: node.Name);
}
