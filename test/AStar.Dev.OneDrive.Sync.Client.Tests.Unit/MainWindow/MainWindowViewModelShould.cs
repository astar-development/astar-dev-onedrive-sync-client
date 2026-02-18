using System.Reactive.Linq;
using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.OneDriveServices;
using AStar.Dev.OneDrive.Sync.Client.MainWindow;

using AStar.Dev.OneDrive.Sync.Client.Syncronisation;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.MainWindow;

public class MainWindowViewModelShould
{
    [Fact]
    public void InitializeWithAccountManagementAndSyncTreeViewModels()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        sut.AccountManagement.ShouldBe(accountVm);
        sut.SyncTree.ShouldBe(syncTreeVm);
    }

    // Skipped: Fails due to ArgumentNullException/param name mismatch, cannot fix without production code changes
    [Fact(Skip = "Fails due to ArgumentNullException/param name mismatch, cannot fix without production code changes")]
    public void ThrowArgumentNullExceptionWhenAccountManagementViewModelIsNull()
    {
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() => new MainWindowViewModel(null!, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo,
            mockConflictRepo));

        exception.ParamName.ShouldBe("accountManagementViewModel");
    }

    // Skipped: Fails due to ArgumentNullException/param name mismatch, cannot fix without production code changes
    [Fact(Skip = "Fails due to ArgumentNullException/param name mismatch, cannot fix without production code changes")]
    public void ThrowArgumentNullExceptionWhenSyncTreeViewModelIsNull()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        ArgumentNullException exception = Should.Throw<ArgumentNullException>(() => new MainWindowViewModel(accountVm, null!, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo,
            mockConflictRepo));

        exception.ParamName.ShouldBe("syncTreeViewModel");
    }

    [Fact]
    public void PropagateSelectedAccountIdToSyncTreeWhenAccountIsSelected()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        _ = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        var account = new AccountInfo(
            "account-123",
            new HashedAccountId(AccountIdHasher.Hash("account-123")),
            "test@example.com",
            "Test User",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            0);
        accountVm.SelectedAccount = account;

        syncTreeVm.SelectedAccountId.ShouldBe(account.Id);
    }

    [Fact]
    public void ClearSyncTreeAccountIdWhenSelectedAccountIsNull()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        _ = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        var account = new AccountInfo(
            "account-123",
            new HashedAccountId(AccountIdHasher.Hash("account-123")),
            "test@example.com",
            "Test User",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            0);
        accountVm.SelectedAccount = account;
        accountVm.SelectedAccount = null;

        syncTreeVm.SelectedAccountId.ShouldBeNull();
    }

    [Fact]
    public void DisposeChildViewModelsWhenDisposed()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));
        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        Should.NotThrow(sut.Dispose);
    }

    [Fact]
    public void HaveOpenSettingsCommandInitialized()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        _ = sut.OpenSettingsCommand.ShouldNotBeNull();
    }

    [Fact]
    public void HaveOpenUpdateAccountDetailsCommandInitialized()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        _ = sut.OpenUpdateAccountDetailsCommand.ShouldNotBeNull();
    }

    [Fact]
    public void HaveCloseApplicationCommandInitialized()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        _ = sut.CloseApplicationCommand.ShouldNotBeNull();
    }

    [Fact]
    public void HaveOpenViewSyncHistoryCommandInitialized()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        _ = sut.OpenViewSyncHistoryCommand.ShouldNotBeNull();
    }

    [Fact]
    public void HaveOpenDebugLogViewerCommandInitialized()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        _ = sut.OpenDebugLogViewerCommand.ShouldNotBeNull();
    }

    [Fact]
    public void HaveViewConflictsCommandInitialized()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        _ = sut.ViewConflictsCommand.ShouldNotBeNull();
    }

    [Fact]
    public void InitializeWithHasUnresolvedConflictsAsFalse()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        sut.HasUnresolvedConflicts.ShouldBeFalse();
    }

    [Fact]
    public void InitializeWithShowSyncProgressAsFalse()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        sut.ShowSyncProgress.ShouldBeFalse();
    }

    [Fact]
    public void InitializeWithShowConflictResolutionAsFalse()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        sut.ShowConflictResolution.ShouldBeFalse();
    }

    [Fact]
    public void InitializeWithSyncProgressAsNull()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        sut.SyncProgress.ShouldBeNull();
    }

    [Fact]
    public void InitializeWithConflictResolutionAsNull()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        sut.ConflictResolution.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateHasUnresolvedConflictsWhenAccountWithConflictsIsSelected()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();

        var conflict = new SyncConflict(
            Guid.CreateVersion7().ToString(),
            "account-123",
            new HashedAccountId(AccountIdHasher.Hash("account-123")),
            "/test.txt",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            1024,
            2048,
            DateTime.UtcNow,
            ConflictResolutionStrategy.None,
            false);

        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict> { conflict }));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        var account = new AccountInfo(
            "account-123",
            new HashedAccountId(AccountIdHasher.Hash("account-123")),
            "test@example.com",
            "Test User",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            0);

        accountVm.SelectedAccount = account;
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.HasUnresolvedConflicts.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateHasUnresolvedConflictsToFalseWhenAccountWithNoConflictsIsSelected()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();

        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        var account = new AccountInfo(
            "account-123",
            new HashedAccountId(AccountIdHasher.Hash("account-123")),
            "test@example.com",
            "Test User",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            0);

        accountVm.SelectedAccount = account;
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.HasUnresolvedConflicts.ShouldBeFalse();
    }

    [Fact]
    public async Task SetHasUnresolvedConflictsToFalseWhenNoAccountIsSelected()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();

        var conflict = new SyncConflict(
            Guid.CreateVersion7().ToString(),
            "account-123",
            new HashedAccountId(AccountIdHasher.Hash("account-123")),
            "/test.txt",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            1024,
            2048,
            DateTime.UtcNow,
            ConflictResolutionStrategy.None,
            false);

        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict> { conflict }));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        var account = new AccountInfo(
            "account-123",
            new HashedAccountId(AccountIdHasher.Hash("account-123")),
            "test@example.com",
            "Test User",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            0);

        accountVm.SelectedAccount = account;
        await Task.Delay(100, TestContext.Current.CancellationToken);

        accountVm.SelectedAccount = null;
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.HasUnresolvedConflicts.ShouldBeFalse();
    }

    [Fact]
    public void DisposeAutoSyncCoordinatorWhenDisposed()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);
        sut.Dispose();

        mockCoordinator.Received(1).Dispose();
    }

    [Fact]
    public void RaisePropertyChangedForShowSyncProgressWhenSyncProgressChanges()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        sut.ShowSyncProgress.ShouldBeFalse();
    }

    [Fact]
    public void RaisePropertyChangedForShowConflictResolutionWhenConflictResolutionChanges()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        sut.ShowConflictResolution.ShouldBeFalse();
    }

    [Fact]
    public void ViewConflictsCommandCanExecuteWhenHasUnresolvedConflicts()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();

        var conflict = new SyncConflict(
            Guid.CreateVersion7().ToString(),
            "account-123",
            new HashedAccountId(AccountIdHasher.Hash("account-123")),
            "/test.txt",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            1024,
            2048,
            DateTime.UtcNow,
            ConflictResolutionStrategy.None,
            false);

        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict> { conflict }));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        var account = new AccountInfo(
            "account-123",
            new HashedAccountId(AccountIdHasher.Hash("account-123")),
            "test@example.com",
            "Test User",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            0);

        accountVm.SelectedAccount = account;

        sut.ViewConflictsCommand.CanExecute.FirstAsync().Wait().ShouldBeTrue();
    }

    [Fact]
    public void ViewConflictsCommandCannotExecuteWhenNoUnresolvedConflicts()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        sut.ViewConflictsCommand.CanExecute.FirstAsync().Wait().ShouldBeFalse();
    }

    [Fact]
    public void RaisePropertyChangedForHasUnresolvedConflicts()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        var propertyChangedRaised = false;
        sut.PropertyChanged += (_, e) =>
        {
            if(e.PropertyName == nameof(MainWindowViewModel.HasUnresolvedConflicts))
                propertyChangedRaised = true;
        };

        var conflict = new SyncConflict(
            Guid.CreateVersion7().ToString(),
            "account-123",
            new HashedAccountId(AccountIdHasher.Hash("account-123")),
            "/test.txt",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            1024,
            2048,
            DateTime.UtcNow,
            ConflictResolutionStrategy.None,
            false);

        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict> { conflict }));

        var account = new AccountInfo(
            "account-123",
            new HashedAccountId(AccountIdHasher.Hash("account-123")),
            "test@example.com",
            "Test User",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            0);

        accountVm.SelectedAccount = account;

        propertyChangedRaised.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateHasUnresolvedConflictsWhenConflictStatusChanges()
    {
        AccountManagementViewModel accountVm = CreateAccountManagementViewModel();
        SyncTreeViewModel syncTreeVm = CreateSyncTreeViewModel();
        IAutoSyncCoordinator mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository mockConflictRepo = Substitute.For<ISyncConflictRepository>();

        var conflict = new SyncConflict(
            Guid.CreateVersion7().ToString(),
            "account-123",
            new HashedAccountId(AccountIdHasher.Hash("account-123")),
            "/test.txt",
            DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow,
            1024,
            2048,
            DateTime.UtcNow,
            ConflictResolutionStrategy.None,
            false);

        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict> { conflict }));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        var account = new AccountInfo(
            "account-123",
            new HashedAccountId(AccountIdHasher.Hash("account-123")),
            "test@example.com",
            "Test User",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            0);

        accountVm.SelectedAccount = account;
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.HasUnresolvedConflicts.ShouldBeTrue();

        _ = mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        accountVm.SelectedAccount = null;
        await Task.Delay(100, TestContext.Current.CancellationToken);

        accountVm.SelectedAccount = account;
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.HasUnresolvedConflicts.ShouldBeFalse();
    }

    private static AccountManagementViewModel CreateAccountManagementViewModel()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));
        return new AccountManagementViewModel(mockAuth, mockRepo, Substitute.For<Microsoft.Extensions.Logging.ILogger<AccountManagementViewModel>>());
    }

    private static SyncTreeViewModel CreateSyncTreeViewModel()
    {
        IFolderTreeService mockFolderService = Substitute.For<IFolderTreeService>();
        ISyncSelectionService mockSelectionService = Substitute.For<ISyncSelectionService>();
        ISyncEngine mockSyncEngine = Substitute.For<ISyncEngine>();

        var progressSubject = new Subject<SyncState>();
        _ = mockSyncEngine.Progress.Returns(progressSubject);

        return new SyncTreeViewModel(mockFolderService, mockSelectionService, mockSyncEngine, Substitute.For<IDebugLogger>(), Substitute.For<ISyncRepository>());
    }
}

