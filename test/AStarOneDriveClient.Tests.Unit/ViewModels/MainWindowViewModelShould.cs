using AStarOneDriveClient.Authentication;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.ViewModels;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;

namespace AStarOneDriveClient.Tests.Unit.ViewModels;

public class MainWindowViewModelShould
{
    [Fact]
    public void InitializeWithAccountManagementAndSyncTreeViewModels()
    {
        var accountVm = CreateAccountManagementViewModel();
        var syncTreeVm = CreateSyncTreeViewModel();

        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>());

        sut.AccountManagement.ShouldBe(accountVm);
        sut.SyncTree.ShouldBe(syncTreeVm);
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenAccountManagementViewModelIsNull()
    {
        var syncTreeVm = CreateSyncTreeViewModel();

        var exception = Should.Throw<ArgumentNullException>(() =>
            new MainWindowViewModel(null!, syncTreeVm, Substitute.For<IServiceProvider>()));

        exception.ParamName.ShouldBe("accountManagementViewModel");
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenSyncTreeViewModelIsNull()
    {
        var accountVm = CreateAccountManagementViewModel();

        var exception = Should.Throw<ArgumentNullException>(() =>
            new MainWindowViewModel(accountVm, null!, Substitute.For<IServiceProvider>()));

        exception.ParamName.ShouldBe("syncTreeViewModel");
    }

    [Fact]
    public void PropagatSelectedAccountIdToSyncTreeWhenAccountIsSelected()
    {
        var accountVm = CreateAccountManagementViewModel();
        var syncTreeVm = CreateSyncTreeViewModel();
        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>());

        var account = new AccountInfo(
            "account-123",
            "test@example.com",
            "Test User",
            true,
            DateTime.UtcNow,
            null,
            false);
        accountVm.SelectedAccount = account;

        syncTreeVm.SelectedAccountId.ShouldBe("account-123");
    }

    [Fact]
    public void ClearSyncTreeAccountIdWhenSelectedAccountIsNull()
    {
        var accountVm = CreateAccountManagementViewModel();
        var syncTreeVm = CreateSyncTreeViewModel();
        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>());

        var account = new AccountInfo(
            "account-123",
            "test@example.com",
            "Test User",
            true,
            DateTime.UtcNow,
            null,
            false);
        accountVm.SelectedAccount = account;
        accountVm.SelectedAccount = null;

        syncTreeVm.SelectedAccountId.ShouldBeNull();
    }

    [Fact]
    public void DisposeChildViewModelsWhenDisposed()
    {
        var accountVm = CreateAccountManagementViewModel();
        var syncTreeVm = CreateSyncTreeViewModel();
        var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>());

        Should.NotThrow(() => sut.Dispose());
    }

    private static AccountManagementViewModel CreateAccountManagementViewModel()
    {
        IAuthService mockAuth = Substitute.For<IAuthService>();
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        mockRepo.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([]));
        return new AccountManagementViewModel(mockAuth, mockRepo);
    }

    private static SyncTreeViewModel CreateSyncTreeViewModel()
    {
        IFolderTreeService mockFolderService = Substitute.For<IFolderTreeService>();
        ISyncSelectionService mockSelectionService = Substitute.For<ISyncSelectionService>();
        ISyncEngine mockSyncEngine = Substitute.For<ISyncEngine>();

        var progressSubject = new System.Reactive.Subjects.Subject<SyncState>();
        mockSyncEngine.Progress.Returns(progressSubject);

        return new SyncTreeViewModel(mockFolderService, mockSelectionService, mockSyncEngine);
    }
}
