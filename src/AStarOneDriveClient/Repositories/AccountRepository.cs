using AStarOneDriveClient.Data;
using AStarOneDriveClient.Data.Entities;
using AStarOneDriveClient.Models;
using Microsoft.EntityFrameworkCore;

namespace AStarOneDriveClient.Repositories;

/// <summary>
/// Repository implementation for managing account data.
/// </summary>
public sealed class AccountRepository : IAccountRepository
{
    private readonly SyncDbContext _context;

    public AccountRepository(SyncDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AccountInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _context.Accounts.ToListAsync(cancellationToken);
        return entities.Select(MapToModel).ToList();
    }

    /// <inheritdoc/>
    public async Task<AccountInfo?> GetByIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        var entity = await _context.Accounts.FindAsync([accountId], cancellationToken);
        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc/>
    public async Task AddAsync(AccountInfo account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        var entity = MapToEntity(account);
        _context.Accounts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(AccountInfo account, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        var entity = await _context.Accounts.FindAsync([account.AccountId], cancellationToken);
        if (entity is null)
        {
            throw new InvalidOperationException($"Account with ID '{account.AccountId}' not found.");
        }

        entity.DisplayName = account.DisplayName;
        entity.LocalSyncPath = account.LocalSyncPath;
        entity.IsAuthenticated = account.IsAuthenticated;
        entity.LastSyncUtc = account.LastSyncUtc;
        entity.DeltaToken = account.DeltaToken;

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        var entity = await _context.Accounts.FindAsync([accountId], cancellationToken);
        if (entity is not null)
        {
            _context.Accounts.Remove(entity);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> ExistsAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        return await _context.Accounts.AnyAsync(a => a.AccountId == accountId, cancellationToken);
    }

    private static AccountInfo MapToModel(AccountEntity entity) =>
        new(
            entity.AccountId,
            entity.DisplayName,
            entity.LocalSyncPath,
            entity.IsAuthenticated,
            entity.LastSyncUtc,
            entity.DeltaToken,
            entity.EnableDetailedSyncLogging
        );

    private static AccountEntity MapToEntity(AccountInfo model) =>
        new()
        {
            AccountId = model.AccountId,
            DisplayName = model.DisplayName,
            LocalSyncPath = model.LocalSyncPath,
            IsAuthenticated = model.IsAuthenticated,
            LastSyncUtc = model.LastSyncUtc,
            DeltaToken = model.DeltaToken,
            EnableDetailedSyncLogging = model.EnableDetailedSyncLogging
        };
}
