using AStar.Dev.OneDrive.Sync.Client.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.Services.Startup;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Startup;

public sealed class GivenAStartupService
{
    private static readonly string AccountId1 = Guid.NewGuid().ToString();
    private static readonly string AccountId2 = Guid.NewGuid().ToString();
    private static readonly string AccountId3 = Guid.NewGuid().ToString();
    private static readonly DateTimeOffset LastSynced = new(2026, 4, 23, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task when_db_is_empty_then_returns_empty_list()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_account_id_not_in_msal_cache_then_account_is_excluded()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([BuildEntity(AccountId1, isActive: true)]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId2]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_account_id_is_in_msal_cache_then_account_is_included()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([BuildEntity(AccountId1, isActive: true)]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task when_account_is_in_cache_then_id_is_mapped_correctly()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([BuildEntity(AccountId1, isActive: true)]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result[0].Id.ShouldBe(AccountId1);
    }

    [Fact]
    public async Task when_account_is_in_cache_then_display_name_is_mapped_correctly()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([BuildEntity(AccountId1, displayName: "Jason Smith", isActive: true)]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result[0].DisplayName.ShouldBe("Jason Smith");
    }

    [Fact]
    public async Task when_account_is_in_cache_then_email_is_mapped_correctly()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([BuildEntity(AccountId1, email: "jason@outlook.com", isActive: true)]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result[0].Email.ShouldBe("jason@outlook.com");
    }

    [Fact]
    public async Task when_account_is_in_cache_then_accent_index_is_mapped_correctly()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([BuildEntity(AccountId1, accentIndex: 3, isActive: true)]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result[0].AccentIndex.ShouldBe(3);
    }

    [Fact]
    public async Task when_account_is_in_cache_then_is_active_is_mapped_correctly()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([BuildEntity(AccountId1, isActive: true)]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result[0].IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task when_account_is_in_cache_then_delta_link_is_mapped_correctly()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([BuildEntity(AccountId1, deltaLink: "https://delta.link/token", isActive: true)]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result[0].DeltaLink.ShouldBe("https://delta.link/token");
    }

    [Fact]
    public async Task when_account_is_in_cache_then_last_synced_at_is_mapped_correctly()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([BuildEntity(AccountId1, lastSyncedAt: LastSynced, isActive: true)]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result[0].LastSyncedAt.ShouldBe(LastSynced);
    }

    [Fact]
    public async Task when_account_is_in_cache_then_quota_total_is_mapped_correctly()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([BuildEntity(AccountId1, quotaTotal: 1_073_741_824L, isActive: true)]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result[0].QuotaTotal.ShouldBe(1_073_741_824L);
    }

    [Fact]
    public async Task when_account_is_in_cache_then_quota_used_is_mapped_correctly()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([BuildEntity(AccountId1, quotaUsed: 536_870_912L, isActive: true)]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result[0].QuotaUsed.ShouldBe(536_870_912L);
    }

    [Fact]
    public async Task when_account_is_in_cache_then_local_sync_path_is_mapped_correctly()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([BuildEntity(AccountId1, localSyncPath: "/home/user/OneDrive", isActive: true)]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result[0].LocalSyncPath.ShouldBe("/home/user/OneDrive");
    }

    [Fact]
    public async Task when_account_is_in_cache_then_conflict_policy_is_mapped_correctly()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns([BuildEntity(AccountId1, conflictPolicy: ConflictPolicy.RemoteWins, isActive: true)]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result[0].ConflictPolicy.ShouldBe(ConflictPolicy.RemoteWins);
    }

    [Fact]
    public async Task when_account_has_sync_folders_then_selected_folder_ids_are_mapped_correctly()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        var entity = BuildEntity(AccountId1, isActive: true);
        entity.SyncFolders.Add(new SyncFolderEntity { FolderId = "folder-a", FolderName = "Photos", AccountId = AccountId1 });
        entity.SyncFolders.Add(new SyncFolderEntity { FolderId = "folder-b", FolderName = "Documents", AccountId = AccountId1 });
        repository.GetAllAsync().Returns([entity]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result[0].SelectedFolderIds.ShouldBe(["folder-a", "folder-b"], ignoreOrder: true);
    }

    [Fact]
    public async Task when_some_accounts_are_cached_and_some_are_not_then_only_cached_accounts_are_returned()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns(
        [
            BuildEntity(AccountId1, isActive: true),
            BuildEntity(AccountId2, isActive: false),
            BuildEntity(AccountId3, isActive: false)
        ]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1, AccountId3]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result.Count.ShouldBe(2);
        result.ShouldAllBe(a => a.Id == AccountId1 || a.Id == AccountId3);
    }

    [Fact]
    public async Task when_multiple_accounts_are_all_active_then_only_first_account_remains_active()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns(
        [
            BuildEntity(AccountId1, isActive: true),
            BuildEntity(AccountId2, isActive: true),
            BuildEntity(AccountId3, isActive: true)
        ]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1, AccountId2, AccountId3]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result.Count(a => a.IsActive).ShouldBe(1);
        result.First(a => a.Id == AccountId1).IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task when_multiple_accounts_are_all_active_then_all_subsequent_accounts_are_set_to_inactive()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns(
        [
            BuildEntity(AccountId1, isActive: true),
            BuildEntity(AccountId2, isActive: true),
            BuildEntity(AccountId3, isActive: true)
        ]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1, AccountId2, AccountId3]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result.Where(a => a.Id != AccountId1).ShouldAllBe(a => !a.IsActive);
    }

    [Fact]
    public async Task when_all_accounts_are_inactive_then_first_account_is_set_active()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns(
        [
            BuildEntity(AccountId1, isActive: false),
            BuildEntity(AccountId2, isActive: false)
        ]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1, AccountId2]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result.First(a => a.Id == AccountId1).IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task when_all_accounts_are_inactive_then_only_first_account_is_set_active()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns(
        [
            BuildEntity(AccountId1, isActive: false),
            BuildEntity(AccountId2, isActive: false)
        ]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1, AccountId2]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result.Count(a => a.IsActive).ShouldBe(1);
    }

    [Fact]
    public async Task when_exactly_one_account_is_active_then_active_state_is_unchanged()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns(
        [
            BuildEntity(AccountId1, isActive: true),
            BuildEntity(AccountId2, isActive: false),
            BuildEntity(AccountId3, isActive: false)
        ]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1, AccountId2, AccountId3]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result.First(a => a.Id == AccountId1).IsActive.ShouldBeTrue();
        result.Count(a => a.IsActive).ShouldBe(1);
    }

    [Fact]
    public async Task when_one_account_is_active_among_many_then_active_account_retains_active_state()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns(
        [
            BuildEntity(AccountId1, isActive: false),
            BuildEntity(AccountId2, isActive: true),
            BuildEntity(AccountId3, isActive: false)
        ]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1, AccountId2, AccountId3]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result.First(a => a.Id == AccountId2).IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task when_one_account_is_active_among_many_then_inactive_accounts_remain_inactive()
    {
        var repository = Substitute.For<IAccountRepository>();
        var authService = Substitute.For<IAuthService>();
        repository.GetAllAsync().Returns(
        [
            BuildEntity(AccountId1, isActive: false),
            BuildEntity(AccountId2, isActive: true),
            BuildEntity(AccountId3, isActive: false)
        ]);
        authService.GetCachedAccountIdsAsync().Returns((IReadOnlyList<string>)[AccountId1, AccountId2, AccountId3]);
        var sut = new StartupService(repository, authService);

        var result = await sut.RestoreAccountsAsync();

        result.Where(a => a.Id != AccountId2).ShouldAllBe(a => !a.IsActive);
    }

    private static AccountEntity BuildEntity(
        string id,
        string displayName = "Test User",
        string email = "test@outlook.com",
        int accentIndex = 0,
        bool isActive = false,
        string? deltaLink = null,
        DateTimeOffset? lastSyncedAt = null,
        long quotaTotal = 0L,
        long quotaUsed = 0L,
        string localSyncPath = "/sync",
        ConflictPolicy conflictPolicy = ConflictPolicy.Ignore) =>
        new()
        {
            Id = id,
            DisplayName = displayName,
            Email = email,
            AccentIndex = accentIndex,
            IsActive = isActive,
            DeltaLink = deltaLink,
            LastSyncedAt = lastSyncedAt,
            QuotaTotal = quotaTotal,
            QuotaUsed = quotaUsed,
            LocalSyncPath = localSyncPath,
            ConflictPolicy = conflictPolicy,
            SyncFolders = []
        };
}
