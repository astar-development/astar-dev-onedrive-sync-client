using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Startup;

public sealed class StartupService(IAccountRepository repository, IAuthService authService) : IStartupService
{
    public async Task<List<OneDriveAccount>> RestoreAccountsAsync()
    {
        List<AccountEntity> entities = await repository.GetAllAsync();

        var cachedIds = (await authService.GetCachedAccountIdsAsync()).ToHashSet();

        List<OneDriveAccount> accounts = [];

        foreach(AccountEntity entity in entities)
        {
            // Skip accounts whose tokens have been evicted from the cache
            // (e.g. user signed out on another device, token expired beyond refresh)
            if(!cachedIds.Contains(entity.Id))
                continue;

            accounts.Add(new OneDriveAccount
            {
                Id = entity.Id,
                DisplayName = entity.DisplayName,
                Email = entity.Email,
                AccentIndex = entity.AccentIndex,
                IsActive = entity.IsActive,
                DeltaLink = entity.DeltaLink,
                LastSyncedAt = entity.LastSyncedAt,
                QuotaTotal = entity.QuotaTotal,
                QuotaUsed = entity.QuotaUsed,
                SelectedFolderIds = [.. entity.SyncFolders.Select(f => f.FolderId)],
                LocalSyncPath = entity.LocalSyncPath,
                ConflictPolicy = entity.ConflictPolicy
            });
        }

        var activeCount = accounts.Count(a => a.IsActive);
        if(activeCount > 1)
        {
            foreach(OneDriveAccount? a in accounts.Where(a => a.IsActive).Skip(1))
                a.IsActive = false;
        }

        if(accounts.Count > 0 && !accounts.Any(a => a.IsActive))
            accounts[0].IsActive = true;

        return accounts;
    }
}
