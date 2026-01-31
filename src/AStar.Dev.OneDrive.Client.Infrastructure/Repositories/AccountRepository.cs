using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

/// <summary>
///     Repository implementation for managing account data.
/// </summary>
public sealed class AccountRepository(SyncDbContext context) : IAccountRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<AccountInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<AccountEntity> entities = await context.Accounts.ToListAsync(cancellationToken);
        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc />
    public async Task<AccountInfo?> GetByIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        AccountEntity? entity = await context.Accounts.FindAsync([accountId], cancellationToken);
        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task AddAsync(AccountInfo account, CancellationToken cancellationToken = default)
    {
        AccountEntity entity = MapToEntity(account);
        _ = context.Accounts.Add(entity);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(AccountInfo account, CancellationToken cancellationToken = default)
    {
        AccountEntity entity = await context.Accounts.FindAsync([account.AccountId], cancellationToken) ?? throw new InvalidOperationException($"Account with ID '{account.AccountId}' not found.");

        entity.DisplayName = account.DisplayName;
        entity.LocalSyncPath = account.LocalSyncPath;
        entity.IsAuthenticated = account.IsAuthenticated;
        entity.LastSyncUtc = account.LastSyncUtc;
        entity.DeltaToken = account.DeltaToken;
        entity.EnableDetailedSyncLogging = account.EnableDetailedSyncLogging;
        entity.EnableDebugLogging = account.EnableDebugLogging;
        entity.MaxParallelUpDownloads = account.MaxParallelUpDownloads;
        entity.MaxItemsInBatch = account.MaxItemsInBatch;
        entity.AutoSyncIntervalMinutes = account.AutoSyncIntervalMinutes;

        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string accountId, CancellationToken cancellationToken = default)
    {
        AccountEntity? entity = await context.Accounts.FindAsync([accountId], cancellationToken);
        if(entity is not null)
        {
            _ = context.Accounts.Remove(entity);
            _ = await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string accountId, CancellationToken cancellationToken = default)
        => await context.Accounts.AnyAsync(a => a.AccountId == accountId, cancellationToken);

    private static AccountInfo MapToModel(AccountEntity entity)
        => new(
            entity.AccountId,
            entity.DisplayName,
            entity.LocalSyncPath,
            entity.IsAuthenticated,
            entity.LastSyncUtc,
            entity.DeltaToken,
            entity.EnableDetailedSyncLogging,
            entity.EnableDebugLogging,
            entity.MaxParallelUpDownloads,
            entity.MaxItemsInBatch,
            entity.AutoSyncIntervalMinutes
        );

    private static AccountEntity MapToEntity(AccountInfo model)
        => new()
        {
            AccountId = model.AccountId,
            DisplayName = model.DisplayName,
            LocalSyncPath = model.LocalSyncPath,
            IsAuthenticated = model.IsAuthenticated,
            LastSyncUtc = model.LastSyncUtc,
            DeltaToken = model.DeltaToken,
            EnableDetailedSyncLogging = model.EnableDetailedSyncLogging,
            EnableDebugLogging = model.EnableDebugLogging,
            MaxParallelUpDownloads = model.MaxParallelUpDownloads,
            MaxItemsInBatch = model.MaxItemsInBatch,
            AutoSyncIntervalMinutes = model.AutoSyncIntervalMinutes
        };
}
