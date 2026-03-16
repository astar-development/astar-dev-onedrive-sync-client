using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Startup;

public interface IStartupService
{
    /// <summary>
    /// Loads all persisted accounts from the database.
    /// Returns them in display order with the previously-active account flagged.
    /// Does NOT attempt any network calls.
    /// </summary>
    Task<List<OneDriveAccount>> RestoreAccountsAsync();
}

public sealed class StartupService(
    IAccountRepository repository,
    IAuthService       authService) : IStartupService
{
    public async Task<List<OneDriveAccount>> RestoreAccountsAsync()
    {
        var entities = await repository.GetAllAsync();

        // Only restore accounts that still have a valid cached MSAL token
        var cachedIds = (await authService.GetCachedAccountIdsAsync()).ToHashSet();

        List<OneDriveAccount> accounts = [];

        foreach (var entity in entities)
        {
            // Skip accounts whose tokens have been evicted from the cache
            // (e.g. user signed out on another device, token expired beyond refresh)
            if (!cachedIds.Contains(entity.Id)) continue;

            accounts.Add(new OneDriveAccount
            {
                Id                = entity.Id,
                DisplayName       = entity.DisplayName,
                Email             = entity.Email,
                AccentIndex       = entity.AccentIndex,
                IsActive          = entity.IsActive,
                DeltaLink         = entity.DeltaLink,
                LastSyncedAt      = entity.LastSyncedAt,
                QuotaTotal        = entity.QuotaTotal,
                QuotaUsed         = entity.QuotaUsed,
                SelectedFolderIds = [.. entity.SyncFolders.Select(f => f.FolderId)]
            });
        }

        // Ensure only one account is active — last-write wins if data is inconsistent
        var activeCount = accounts.Count(a => a.IsActive);
        if (activeCount > 1)
        {
            foreach (var a in accounts.Where(a => a.IsActive).Skip(1))
                a.IsActive = false;
        }

        return accounts;
    }
}
