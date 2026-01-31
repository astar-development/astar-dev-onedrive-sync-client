using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

/// <summary>
///     Repository implementation for managing sync configuration data.
/// </summary>
public sealed class SyncConfigurationRepository(SyncDbContext context) : ISyncConfigurationRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncConfiguration>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        List<SyncConfigurationEntity> entities = await context.SyncConfigurations
            .Where(sc => sc.AccountId == accountId)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetSelectedFoldersAsync(string accountId, CancellationToken cancellationToken = default)
        => await context.SyncConfigurations
            .Where(sc => sc.AccountId == accountId && sc.IsSelected)
            .Select(sc => CleanUpPath(sc.FolderPath))
            .Distinct()
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<Result<IList<string>, ErrorResponse>> GetSelectedFolders2Async(string accountId, CancellationToken cancellationToken = default)
        => await context.SyncConfigurations
            .Where(sc => sc.AccountId == accountId && sc.IsSelected)
            .Select(sc => CleanUpPath(sc.FolderPath))
            .Distinct()
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<SyncConfiguration> AddAsync(SyncConfiguration configuration, CancellationToken cancellationToken = default)
    {
        SyncConfigurationEntity? existingEntity = await context.SyncConfigurations
            .FirstOrDefaultAsync(sc => sc.AccountId == configuration.AccountId && sc.FolderPath == configuration.FolderPath, cancellationToken);

        if(existingEntity is not null)
            return configuration;

        SyncConfigurationEntity entity = MapToEntity(configuration);
        _ = context.SyncConfigurations.Add(entity);
        _ = await context.SaveChangesAsync(cancellationToken);

        return configuration;
    }

    /// <inheritdoc />
    public async Task UpdateAsync(SyncConfiguration configuration, CancellationToken cancellationToken = default)
    {
        SyncConfigurationEntity entity = await context.SyncConfigurations.FindAsync([configuration.Id], cancellationToken) ??
                                         throw new InvalidOperationException($"Sync configuration with ID '{configuration.Id}' not found.");

        entity.AccountId = configuration.AccountId;
        entity.FolderPath = configuration.FolderPath;
        entity.IsSelected = configuration.IsSelected;
        entity.LastModifiedUtc = configuration.LastModifiedUtc;

        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        SyncConfigurationEntity? entity = await context.SyncConfigurations.FindAsync([id], cancellationToken);
        if(entity is not null)
        {
            _ = context.SyncConfigurations.Remove(entity);
            _ = await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task DeleteByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        List<SyncConfigurationEntity> entities = await context.SyncConfigurations
            .Where(sc => sc.AccountId == accountId)
            .ToListAsync(cancellationToken);

        context.SyncConfigurations.RemoveRange(entities);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveBatchAsync(string accountId, IEnumerable<SyncConfiguration> configurations, CancellationToken cancellationToken = default)
    {
        List<SyncConfigurationEntity> existingEntities = await context.SyncConfigurations
            .Where(sc => sc.AccountId == accountId)
            .ToListAsync(cancellationToken);

        context.SyncConfigurations.RemoveRange(existingEntities);

        var newEntities = configurations.Select(MapToEntity).ToList();
        context.SyncConfigurations.AddRange(newEntities);

        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SyncConfigurationEntity?> GetParentFolderAsync(string accountId, string parentPath, string possibleParentPath, CancellationToken cancellationToken)
    {
        SyncConfigurationEntity? parentEntity = await context.SyncConfigurations
            .FirstOrDefaultAsync(sc => sc.AccountId == accountId && (sc.FolderPath == parentPath || sc.FolderPath == possibleParentPath), cancellationToken);

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

    private static SyncConfiguration MapToModel(SyncConfigurationEntity entity)
        => new(
            entity.Id,
            entity.AccountId,
            entity.FolderPath,
            entity.IsSelected,
            entity.LastModifiedUtc
        );

    private static SyncConfigurationEntity MapToEntity(SyncConfiguration model)
        => new()
        {
            Id = model.Id,
            AccountId = model.AccountId,
            FolderPath = model.FolderPath,
            IsSelected = model.IsSelected,
            LastModifiedUtc = model.LastModifiedUtc
        };
}
