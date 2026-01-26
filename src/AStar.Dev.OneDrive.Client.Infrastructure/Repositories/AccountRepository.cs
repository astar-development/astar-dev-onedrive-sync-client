using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Repositories;

/// <summary>
///     Repository implementation for managing account data.
/// </summary>
public sealed class AccountRepository(IDbContextFactory<SyncDbContext> contextFactory) : IAccountRepository
{
    private readonly IDbContextFactory<SyncDbContext> _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));

    /// <inheritdoc />
    public async Task<IReadOnlyList<AccountInfo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        List<AccountEntity> entities = await context.Accounts.Where(account => account.DisplayName != "System Admin").ToListAsync(cancellationToken);
        return [.. entities.Select(MapToModel)];
    }

    /// <inheritdoc />
    public async Task<AccountInfo?> GetByIdAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        AccountEntity? entity = await context.Accounts.AsNoTracking().FirstOrDefaultAsync(a => a.AccountId == accountId, cancellationToken);
        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task AddAsync(AccountInfo account, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        if(IsOneOfMyAccounts(account))
        {
            account = account with { EnableDebugLogging = true, EnableDetailedSyncLogging = true, MaxParallelUpDownloads = 5 };
        }

        AccountEntity entity = MapToEntity(account);
        _ = context.Accounts.Add(entity);
        _ = await context.SaveChangesAsync(cancellationToken);
    }

    private static bool IsOneOfMyAccounts(AccountInfo account)
    => (account.DisplayName.StartsWith("jason.", StringComparison.OrdinalIgnoreCase) && account.DisplayName.EndsWith("@outlook.com", StringComparison.OrdinalIgnoreCase))
        || account.DisplayName.Equals("astar-development@outlook.com", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task UpdateAsync(AccountInfo account, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        AccountEntity dbAccount = await context.Accounts.FindAsync([account.AccountId], cancellationToken) ?? throw new InvalidOperationException($"Account with ID '{account.AccountId}' not found.");

        dbAccount.DisplayName = account.DisplayName;
        dbAccount.LocalSyncPath = account.LocalSyncPath;
        dbAccount.IsAuthenticated = account.IsAuthenticated;
        dbAccount.LastSyncUtc = account.LastSyncUtc;
        dbAccount.DeltaToken = account.DeltaToken;
        dbAccount.EnableDetailedSyncLogging = account.EnableDetailedSyncLogging;
        dbAccount.EnableDebugLogging = account.EnableDebugLogging;
        dbAccount.MaxParallelUpDownloads = account.MaxParallelUpDownloads;
        dbAccount.MaxItemsInBatch = account.MaxItemsInBatch;
        dbAccount.AutoSyncIntervalMinutes = account.AutoSyncIntervalMinutes;

        _ = await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();
        AccountEntity? entity = await context.Accounts.FindAsync([accountId], cancellationToken);
        if(entity is not null)
        {
            _ = context.Accounts.Remove(entity);
            _ = await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string accountId, CancellationToken cancellationToken = default)
    {
        await using SyncDbContext context = _contextFactory.CreateDbContext();

        return await context.Accounts.AnyAsync(a => a.AccountId == accountId, cancellationToken);
    }

    private static AccountInfo MapToModel(AccountEntity account)
        => new(
            account.AccountId,
            account.DisplayName,
            account.LocalSyncPath,
            account.IsAuthenticated,
            account.LastSyncUtc,
            account.DeltaToken,
            account.EnableDetailedSyncLogging,
            account.EnableDebugLogging,
            account.MaxParallelUpDownloads,
            account.MaxItemsInBatch,
            account.AutoSyncIntervalMinutes
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
