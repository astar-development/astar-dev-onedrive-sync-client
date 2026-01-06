using AStarOneDriveClient.Authentication;
using AStarOneDriveClient.Data;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.ViewModels;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using System.Reactive.Subjects;

namespace AStarOneDriveClient.Tests.Unit.ViewModels;

/// <summary>
/// Integration tests for MainWindowViewModel coordinating AccountManagementViewModel and SyncTreeViewModel.
/// </summary>
public class MainWindowViewModelIntegrationShould : IDisposable
{
    private readonly SyncDbContext _context;
    private readonly AccountRepository _accountRepository;
    private readonly IAuthService _mockAuthService;
    private readonly IFolderTreeService _mockFolderTreeService;
    private readonly ISyncSelectionService _syncSelectionService;
    private readonly ISyncEngine _mockSyncEngine;
    private readonly Subject<SyncState> _progressSubject;

    public MainWindowViewModelIntegrationShould()
    {
        var options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        _context = new SyncDbContext(options);
        _accountRepository = new AccountRepository(_context);
        _mockAuthService = Substitute.For<IAuthService>();
        _mockFolderTreeService = Substitute.For<IFolderTreeService>();
        _syncSelectionService = new SyncSelectionService();
        _mockSyncEngine = Substitute.For<ISyncEngine>();

        _progressSubject = new Subject<SyncState>();
        _mockSyncEngine.Progress.Returns(_progressSubject);

        // Setup folder tree service to return empty list by default
        _mockFolderTreeService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>([]));
    }

    [Fact]
    public async Task LoadFoldersWhenAccountIsSelected()
    {
        // Arrange
        var account = new AccountInfo(
            "acc-123",
            "test@example.com",
            @"C:\Sync",
            true,
            null,
            null,
            false);
        await _accountRepository.AddAsync(account);

        var rootFolder = new OneDriveFolderNode
        {
            Id = "folder1",
            Name = "Documents",
            Path = "/Documents",
            IsFolder = true
        };
        _mockFolderTreeService.GetRootFoldersAsync("acc-123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>([rootFolder]));

        var accountVm = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        var syncTreeVm = new SyncTreeViewModel(_mockFolderTreeService, _syncSelectionService, _mockSyncEngine);
        using var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>());

        // Allow initialization to complete
        await Task.Delay(100);

        // Act
        accountVm.SelectedAccount = account;
        await Task.Delay(100);

        // Assert
        syncTreeVm.SelectedAccountId.ShouldBe("acc-123");
        syncTreeVm.RootFolders.ShouldNotBeEmpty();
        syncTreeVm.RootFolders.Count.ShouldBe(1);
        syncTreeVm.RootFolders[0].Name.ShouldBe("Documents");
    }

    [Fact]
    public async Task ClearFoldersWhenAccountIsDeselected()
    {
        // Arrange
        var account = new AccountInfo(
            "acc-456",
            "user@example.com",
            @"C:\Sync2",
            true,
            null,
            null,
            false);
        await _accountRepository.AddAsync(account);

        var rootFolder = new OneDriveFolderNode
        {
            Id = "folder1",
            Name = "Photos",
            Path = "/Photos",
            IsFolder = true
        };
        _mockFolderTreeService.GetRootFoldersAsync("acc-456", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>([rootFolder]));

        var accountVm = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        var syncTreeVm = new SyncTreeViewModel(_mockFolderTreeService, _syncSelectionService, _mockSyncEngine);
        using var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>());

        await Task.Delay(100);
        accountVm.SelectedAccount = account;
        await Task.Delay(100);

        // Act
        accountVm.SelectedAccount = null;
        await Task.Delay(100);

        // Assert
        syncTreeVm.SelectedAccountId.ShouldBeNull();
        syncTreeVm.RootFolders.ShouldBeEmpty();
    }

    [Fact]
    public async Task SwitchFoldersWhenDifferentAccountIsSelected()
    {
        // Arrange
        var account1 = new AccountInfo("acc-1", "user1@example.com", @"C:\Sync1", true, null, null, false);
        var account2 = new AccountInfo("acc-2", "user2@example.com", @"C:\Sync2", true, null, null, false);
        await _accountRepository.AddAsync(account1);
        await _accountRepository.AddAsync(account2);

        var folder1 = new OneDriveFolderNode
        {
            Id = "f1",
            Name = "Folder1",
            Path = "/Folder1",
            IsFolder = true
        };
        var folder2 = new OneDriveFolderNode
        {
            Id = "f2",
            Name = "Folder2",
            Path = "/Folder2",
            IsFolder = true
        };

        _mockFolderTreeService.GetRootFoldersAsync("acc-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>([folder1]));
        _mockFolderTreeService.GetRootFoldersAsync("acc-2", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>([folder2]));

        var accountVm = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        var syncTreeVm = new SyncTreeViewModel(_mockFolderTreeService, _syncSelectionService, _mockSyncEngine);
        using var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>());

        await Task.Delay(100);

        // Act - Select first account
        accountVm.SelectedAccount = account1;
        await Task.Delay(100);
        var firstFolder = syncTreeVm.RootFolders.FirstOrDefault()?.Name;

        // Act - Select second account
        accountVm.SelectedAccount = account2;
        await Task.Delay(100);
        var secondFolder = syncTreeVm.RootFolders.FirstOrDefault()?.Name;

        // Assert
        firstFolder.ShouldBe("Folder1");
        secondFolder.ShouldBe("Folder2");
        syncTreeVm.SelectedAccountId.ShouldBe("acc-2");
    }

    [Fact]
    public async Task HandleErrorsGracefullyWhenFolderLoadingFails()
    {
        // Arrange
        var account = new AccountInfo("acc-999", "error@example.com", @"C:\Sync", true, null, null, false);
        await _accountRepository.AddAsync(account);

        _mockFolderTreeService.GetRootFoldersAsync("acc-999", Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<OneDriveFolderNode>>(
                new InvalidOperationException("Network error")));

        var accountVm = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        var syncTreeVm = new SyncTreeViewModel(_mockFolderTreeService, _syncSelectionService, _mockSyncEngine);
        using var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>());

        await Task.Delay(100);

        // Act
        accountVm.SelectedAccount = account;
        await Task.Delay(100);

        // Assert
        syncTreeVm.ErrorMessage.ShouldNotBeNullOrEmpty();
        syncTreeVm.RootFolders.ShouldBeEmpty();
    }

    [Fact]
    public async Task LoadFreshFolderInstancesWhenAccountIsReselected()
    {
        // Arrange
        var account = new AccountInfo("acc-sel", "sel@example.com", @"C:\Sync", true, null, null, false);
        await _accountRepository.AddAsync(account);

        // Create new instances each time to simulate fresh load from API
        _mockFolderTreeService.GetRootFoldersAsync("acc-sel", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<IReadOnlyList<OneDriveFolderNode>>([
                new OneDriveFolderNode
                {
                    Id = "f1",
                    Name = "TestFolder",
                    Path = "/TestFolder",
                    IsFolder = true
                }
            ]));

        var accountVm = new AccountManagementViewModel(_mockAuthService, _accountRepository);
        var syncTreeVm = new SyncTreeViewModel(_mockFolderTreeService, _syncSelectionService, _mockSyncEngine);
        using var sut = new MainWindowViewModel(accountVm, syncTreeVm, Substitute.For<IServiceProvider>());

        await Task.Delay(100);
        accountVm.SelectedAccount = account;
        await Task.Delay(100);

        // Act - Select the folder
        var folderToSelect = syncTreeVm.RootFolders[0];
        syncTreeVm.ToggleSelectionCommand.Execute(folderToSelect).Subscribe();
        await Task.Delay(50);

        // Store the selection state
        var initialState = folderToSelect.SelectionState;

        // Deselect and reselect account
        accountVm.SelectedAccount = null;
        await Task.Delay(100);
        accountVm.SelectedAccount = account;
        await Task.Delay(100);

        // Assert - Fresh instances should have reset selection state
        var reloadedFolder = syncTreeVm.RootFolders[0];
        reloadedFolder.SelectionState.ShouldBe(SelectionState.Unchecked);
        initialState.ShouldBe(SelectionState.Checked);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
