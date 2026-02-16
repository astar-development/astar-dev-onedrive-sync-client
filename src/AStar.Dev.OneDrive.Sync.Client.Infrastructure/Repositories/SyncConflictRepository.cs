using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;

/// <summary>
///     Repository for managing sync conflicts in the database.
/// </summary>
public sealed class SyncConflictRepository(IDbContextFactory<SyncDbContext> contextFactory) : ISyncConflictRepository
{
    private readonly IDbContextFactory<SyncDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncConflict>> GetByAccountIdAsync(HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<SyncConflictEntity> entities = await context.SyncConflicts
            .Where(c => c.HashedAccountId == hashedAccountId)
            .OrderByDescending(c => c.DetectedUtc)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToDomain)];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncConflict>> GetUnresolvedByAccountIdAsync(HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<SyncConflictEntity> entities = await context.SyncConflicts
            .Where(c => c.HashedAccountId == hashedAccountId && !c.IsResolved)
            .OrderByDescending(c => c.DetectedUtc)
            .ToListAsync(cancellationToken);

        return [.. entities.Select(MapToDomain)];
    }

    /// <inheritdoc />
    public async Task<SyncConflict?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        SyncConflictEntity? entity = await context.SyncConflicts
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return entity is not null ? MapToDomain(entity) : null;
    }

    /// <inheritdoc />
    public async Task<SyncConflict?> GetByFilePathAsync(HashedAccountId hashedAccountId, string filePath, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        SyncConflictEntity? entity = await context.SyncConflicts
            .Where(c => c.HashedAccountId == hashedAccountId && c.FilePath == filePath && !c.IsResolved)
            .OrderByDescending(c => c.DetectedUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is not null ? MapToDomain(entity) : null;
    }

    /// <inheritdoc />
    public async Task AddAsync(SyncConflict conflict, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        SyncConflictEntity entity = MapToEntity(conflict);
        _ = await context.SyncConflicts.AddAsync(entity, cancellationToken);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(SyncConflict conflict, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        SyncConflictEntity existingSyncConflict = await context.SyncConflicts
            .FirstOrDefaultAsync(c => c.Id == conflict.Id, cancellationToken) ?? throw new InvalidOperationException($"Conflict not found: {conflict.Id}");

        existingSyncConflict.ResolutionStrategy = conflict.ResolutionStrategy;
        existingSyncConflict.IsResolved = conflict.IsResolved;

        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        SyncConflictEntity? SyncConflict = await context.SyncConflicts
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if(SyncConflict is not null)
        {
            _ = context.SyncConflicts.Remove(SyncConflict);
            _ = await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task DeleteByAccountIdAsync(HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        _ = await context.SyncConflicts
        .Where(c => c.HashedAccountId == hashedAccountId)
        .ExecuteDeleteAsync(cancellationToken);
    }

    private static SyncConflict MapToDomain(SyncConflictEntity syncConflict) => new(
                syncConflict.Id,
                syncConflict.HashedAccountId,
                syncConflict.FilePath,
                syncConflict.LocalModifiedUtc,
                syncConflict.RemoteModifiedUtc,
                syncConflict.LocalSize,
                syncConflict.RemoteSize,
                syncConflict.DetectedUtc,
                syncConflict.ResolutionStrategy,
                syncConflict.IsResolved);

    private static SyncConflictEntity MapToEntity(SyncConflict conflict) => new()
    {
        Id = conflict.Id,
        HashedAccountId = conflict.HashedAccountId,
        FilePath = conflict.FilePath,
        LocalModifiedUtc = conflict.LocalModifiedUtc,
        RemoteModifiedUtc = conflict.RemoteModifiedUtc,
        LocalSize = conflict.LocalSize,
        RemoteSize = conflict.RemoteSize,
        DetectedUtc = conflict.DetectedUtc,
        ResolutionStrategy = conflict.ResolutionStrategy,
        IsResolved = conflict.IsResolved
    };
}
