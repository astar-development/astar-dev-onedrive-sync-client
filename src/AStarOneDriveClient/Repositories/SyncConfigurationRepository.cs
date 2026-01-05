using AStarOneDriveClient.Data;
using AStarOneDriveClient.Data.Entities;
using AStarOneDriveClient.Models;
using Microsoft.EntityFrameworkCore;

namespace AStarOneDriveClient.Repositories;

/// <summary>
/// Repository implementation for managing sync configuration data.
/// </summary>
public sealed class SyncConfigurationRepository : ISyncConfigurationRepository
{
    private readonly SyncDbContext _context;

    public SyncConfigurationRepository(SyncDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncConfiguration>> GetByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        var entities = await _context.SyncConfigurations
            .Where(sc => sc.AccountId == accountId)
            .ToListAsync(cancellationToken);

        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<string>> GetSelectedFoldersAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        return await _context.SyncConfigurations
            .Where(sc => sc.AccountId == accountId && sc.IsSelected)
            .Select(sc => sc.FolderPath)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task AddAsync(SyncConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var entity = MapToEntity(configuration);
        _context.SyncConfigurations.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(SyncConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var entity = await _context.SyncConfigurations.FindAsync([configuration.Id], cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException($"Sync configuration with ID '{configuration.Id}' not found.");
        }

        entity.AccountId = configuration.AccountId;
        entity.FolderPath = configuration.FolderPath;
        entity.IsSelected = configuration.IsSelected;
        entity.LastModifiedUtc = configuration.LastModifiedUtc;

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.SyncConfigurations.FindAsync([id], cancellationToken);
        if (entity is not null)
        {
            _context.SyncConfigurations.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteByAccountIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        var entities = await _context.SyncConfigurations
            .Where(sc => sc.AccountId == accountId)
            .ToListAsync(cancellationToken);

        _context.SyncConfigurations.RemoveRange(entities);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task SaveBatchAsync(string accountId, IEnumerable<SyncConfiguration> configurations, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(configurations);

        var existingEntities = await _context.SyncConfigurations
            .Where(sc => sc.AccountId == accountId)
            .ToListAsync(cancellationToken);

        _context.SyncConfigurations.RemoveRange(existingEntities);

        var newEntities = configurations.Select(MapToEntity).ToList();
        _context.SyncConfigurations.AddRange(newEntities);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static SyncConfiguration MapToModel(SyncConfigurationEntity entity) =>
        new(
            entity.Id,
            entity.AccountId,
            entity.FolderPath,
            entity.IsSelected,
            entity.LastModifiedUtc
        );

    private static SyncConfigurationEntity MapToEntity(SyncConfiguration model) =>
        new()
        {
            Id = model.Id,
            AccountId = model.AccountId,
            FolderPath = model.FolderPath,
            IsSelected = model.IsSelected,
            LastModifiedUtc = model.LastModifiedUtc
        };
}
