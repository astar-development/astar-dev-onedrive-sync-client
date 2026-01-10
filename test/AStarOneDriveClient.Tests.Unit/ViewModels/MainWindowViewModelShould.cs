using AStarOneDriveClient.Authentication;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.ViewModels;

namespace AStarOneDriveClient.Tests.Unit.ViewModels;

public class MainWindowViewModelShould
{
    [Fact]
    public void InitializeWithAccountManagementAndSyncTreeViewModels()
    {
        var accountVm = CreateAccountManagementViewModel();
        var syncTreeVm = CreateSyncTreeViewModel();
        var mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        sut.AccountManagement.ShouldBe(accountVm);
        sut.SyncTree.ShouldBe(syncTreeVm);
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenAccountManagementViewModelIsNull()
    {
        var syncTreeVm = CreateSyncTreeViewModel();
        var mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var exception = Should.Throw<ArgumentNullException>(() =>
            new MainWindowViewModel(null!, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo));

        exception.ParamName.ShouldBe("accountManagementViewModel");
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenSyncTreeViewModelIsNull()
    {
        var accountVm = CreateAccountManagementViewModel();
        var mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        var exception = Should.Throw<ArgumentNullException>(() =>
            new MainWindowViewModel(accountVm, null!, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo));

        exception.ParamName.ShouldBe("syncTreeViewModel");
    }

    [Fact]
    public void PropagatSelectedAccountIdToSyncTreeWhenAccountIsSelected()
    {
        var accountVm = CreateAccountManagementViewModel();
        var syncTreeVm = CreateSyncTreeViewModel();
        var mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        _ = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        var account = new AccountInfo(
            "account-123",
            "test@example.com",
            "Test User",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            null);
        accountVm.SelectedAccount = account;

        syncTreeVm.SelectedAccountId.ShouldBe("account-123");
    }

    [Fact]
    public void ClearSyncTreeAccountIdWhenSelectedAccountIsNull()
    {
        var accountVm = CreateAccountManagementViewModel();
        var syncTreeVm = CreateSyncTreeViewModel();
        var mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));

        _ = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        var account = new AccountInfo(
            "account-123",
            "test@example.com",
            "Test User",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            null);
        accountVm.SelectedAccount = account;
        accountVm.SelectedAccount = null;

        syncTreeVm.SelectedAccountId.ShouldBeNull();
    }

    [Fact]
    public void DisposeChildViewModelsWhenDisposed()
    {
        var accountVm = CreateAccountManagementViewModel();
        var syncTreeVm = CreateSyncTreeViewModel();
        var mockCoordinator = Substitute.For<IAutoSyncCoordinator>();
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockConflictRepo = Substitute.For<ISyncConflictRepository>();
        mockConflictRepo.GetUnresolvedByAccountIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<SyncConflict>>(new List<SyncConflict>()));
        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>(), mockCoordinator, mockRepo, mockConflictRepo);

        Should.NotThrow(sut.Dispose);
    }

    private static AccountManagementViewModel CreateAccountManagementViewModel()
    {
        var mockAuth = Substitute.For<IAuthService>();
        var mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));
        return new AccountManagementViewModel(mockAuth, mockRepo);
    }

    private static SyncTreeViewModel CreateSyncTreeViewModel()
    {
        var mockFolderService = Substitute.For<IFolderTreeService>();
        var mockSelectionService = Substitute.For<ISyncSelectionService>();
        var mockSyncEngine = Substitute.For<ISyncEngine>();

        var progressSubject = new System.Reactive.Subjects.Subject<SyncState>();
        mockSyncEngine.Progress.Returns(progressSubject);

        return new SyncTreeViewModel(mockFolderService, mockSelectionService, mockSyncEngine);
    }
}
