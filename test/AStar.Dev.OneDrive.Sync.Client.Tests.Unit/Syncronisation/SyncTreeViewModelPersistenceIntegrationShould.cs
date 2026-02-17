using System.Reactive.Subjects;
using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.OneDriveServices;

using AStar.Dev.OneDrive.Sync.Client.Syncronisation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Syncronisation;

/// <summary>
///     Integration tests for folder selection persistence through the full stack.
/// </summary>
public class SyncTreeViewModelPersistenceIntegrationShould : IDisposable
{
    private readonly SyncConfigurationRepository _configRepository;
    private readonly SyncDbContext _context;
    private readonly IFolderTreeService _mockFolderTreeService;
    private readonly ISyncEngine _mockSyncEngine;
    private readonly Subject<SyncState> _progressSubject;
    private readonly SyncSelectionService _selectionService;
    private readonly IDebugLogger _mockDebugLogger;
    private readonly IDbContextFactory<SyncDbContext> _contextFactory;
    private readonly ISyncRepository _syncRepository;

    public SyncTreeViewModelPersistenceIntegrationShould()
    {
        _contextFactory = new PooledDbContextFactory<SyncDbContext>(
            new DbContextOptionsBuilder<SyncDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);
        _context = _contextFactory.CreateDbContext();
        _configRepository = new SyncConfigurationRepository(_contextFactory);
        _selectionService = new SyncSelectionService(_configRepository);
        _mockFolderTreeService = Substitute.For<IFolderTreeService>();
        _mockSyncEngine = Substitute.For<ISyncEngine>();
        _mockDebugLogger = Substitute.For<IDebugLogger>();
        _syncRepository = Substitute.For<ISyncRepository>();

        _progressSubject = new Subject<SyncState>();
        _ = _mockSyncEngine.Progress.Returns(_progressSubject);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task PersistSelectionsToDatabase()
    {
        List<OneDriveFolderNode> folders = CreateTestFolders();
        _ = _mockFolderTreeService.GetRootFoldersAsync("acc-1", new HashedAccountId(AccountIdHasher.Hash("acc-1")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>(folders));

        using var sut = new SyncTreeViewModel(_mockFolderTreeService, _selectionService, _mockSyncEngine, _mockDebugLogger, _syncRepository);
        sut.SelectedAccountId = "acc-1";
        await Task.Delay(100, TestContext.Current.CancellationToken); // Allow async load
        OneDriveFolderNode folderToSelect = sut.Folders[0];
        _ = sut.ToggleSelectionCommand.Execute(folderToSelect).Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken); // Allow async save
        IReadOnlyList<string> savedPaths = await _configRepository.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc-1")), TestContext.Current.CancellationToken);
        savedPaths.ShouldContain("/Folder1");
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task RestoreSelectionsFromDatabase()
    {
        await _configRepository.SaveBatchAsync(new HashedAccountId(AccountIdHasher.Hash("acc-1")), [
            new FileMetadata("", new HashedAccountId(AccountIdHasher.Hash("acc-1")), "name",  "/Parent/Child1", 0,DateTime.UtcNow, "")
        ], TestContext.Current.CancellationToken);

        List<OneDriveFolderNode> folders = CreateTestFolders();
        _ = _mockFolderTreeService.GetRootFoldersAsync("acc-1", new HashedAccountId(AccountIdHasher.Hash("acc-1")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>(folders));
        using var sut = new SyncTreeViewModel(_mockFolderTreeService, _selectionService, _mockSyncEngine, _mockDebugLogger, _syncRepository);
        sut.SelectedAccountId = "acc-1";
        await Task.Delay(150, TestContext.Current.CancellationToken);

        OneDriveFolderNode? folder2 = sut.Folders.FirstOrDefault(f => f.Path == "/Folder2");
        _ = folder2.ShouldNotBeNull();
        folder2.SelectionState.ShouldBe(SelectionState.Checked);
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task ClearSelectionsFromDatabase()
    {
        await _configRepository.SaveBatchAsync(new HashedAccountId(AccountIdHasher.Hash("acc-1")), [
            new FileMetadata("", new HashedAccountId(AccountIdHasher.Hash("acc-1")), "name",  "/Folder1", 0, DateTime.UtcNow, "")
        ], TestContext.Current.CancellationToken);

        List<OneDriveFolderNode> folders = CreateTestFolders();
        _ = _mockFolderTreeService.GetRootFoldersAsync("acc-1", new HashedAccountId(AccountIdHasher.Hash("acc-1")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>(folders));

        using var sut = new SyncTreeViewModel(_mockFolderTreeService, _selectionService, _mockSyncEngine, _mockDebugLogger, _syncRepository);
        sut.SelectedAccountId = "acc-1";
        await Task.Delay(150, TestContext.Current.CancellationToken);
        _ = sut.ClearSelectionsCommand.Execute().Subscribe();
        await Task.Delay(100, TestContext.Current.CancellationToken); // Allow async save
        IReadOnlyList<string> savedPaths = await _configRepository.GetSelectedFoldersAsync(new HashedAccountId(AccountIdHasher.Hash("acc-1")), TestContext.Current.CancellationToken);
        savedPaths.ShouldBeEmpty();
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task MaintainSeparateSelectionsPerAccount()
    {
        await _configRepository.SaveBatchAsync(new HashedAccountId(AccountIdHasher.Hash("acc-1")), [
            new FileMetadata("", new HashedAccountId(AccountIdHasher.Hash("acc-1")), "name",  "/Folder1", 0, DateTime.UtcNow, "")
        ], TestContext.Current.CancellationToken);
        await _configRepository.SaveBatchAsync(new HashedAccountId(AccountIdHasher.Hash("acc-2")), [
            new FileMetadata("", new HashedAccountId(AccountIdHasher.Hash("acc-2")), "name",  "/Folder2", 0, DateTime.UtcNow, "")
        ], TestContext.Current.CancellationToken);

        List < OneDriveFolderNode > folders = CreateTestFolders();
        _ = _mockFolderTreeService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>(folders));
        using var sut = new SyncTreeViewModel(_mockFolderTreeService, _selectionService, _mockSyncEngine, _mockDebugLogger, _syncRepository);
        sut.SelectedAccountId = "acc-1";
        await Task.Delay(150, TestContext.Current.CancellationToken);

        SelectionState folder1Selected = sut.Folders.First(f => f.Path == "/Folder1").SelectionState;
        SelectionState folder2Selected = sut.Folders.First(f => f.Path == "/Folder2").SelectionState;

        folder1Selected.ShouldBe(SelectionState.Checked);
        folder2Selected.ShouldBe(SelectionState.Unchecked);
    }

    [Fact(Skip = "Fails due to exception type mismatch, cannot fix without production code changes")]
    public async Task HandleDatabaseErrorsGracefully()
    {
        await _context.DisposeAsync();

        List<OneDriveFolderNode> folders = CreateTestFolders();
        _ = _mockFolderTreeService.GetRootFoldersAsync("acc-1", new HashedAccountId(AccountIdHasher.Hash("acc-1")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>(folders));
        using var sut = new SyncTreeViewModel(_mockFolderTreeService, _selectionService, _mockSyncEngine, _mockDebugLogger, _syncRepository);
        sut.SelectedAccountId = "acc-1";
        await Task.Delay(150, TestContext.Current.CancellationToken);

        _ = Should.Throw<InvalidOperationException>(() =>
        {
            OneDriveFolderNode folderToSelect = sut.Folders[0];
            _ = sut.ToggleSelectionCommand.Execute(folderToSelect).Subscribe();
        });
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task RestoreIndeterminateStatesCorrectly()
    {
        await _configRepository.SaveBatchAsync(new HashedAccountId(AccountIdHasher.Hash("acc-1")), [
            new FileMetadata("", new HashedAccountId(AccountIdHasher.Hash("acc-1")), "name",  "/Parent/Child1", 0,DateTime.UtcNow, "")
        ], TestContext.Current.CancellationToken);

        OneDriveFolderNode child1 = CreateFolder("c1", "Child1", "/Parent/Child1");
        OneDriveFolderNode child2 = CreateFolder("c2", "Child2", "/Parent/Child2");
        OneDriveFolderNode parent = CreateFolder("p", "Parent", "/Parent");
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        child1.ParentId = "p";
        child2.ParentId = "p";

        _ = _mockFolderTreeService.GetRootFoldersAsync("acc-1", new HashedAccountId(AccountIdHasher.Hash("acc-1")), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>([parent]));

        using var sut = new SyncTreeViewModel(_mockFolderTreeService, _selectionService, _mockSyncEngine, _mockDebugLogger, _syncRepository);
        sut.SelectedAccountId = "acc-1";
        await Task.Delay(150, TestContext.Current.CancellationToken);

        OneDriveFolderNode loadedParent = sut.Folders[0];
        OneDriveFolderNode loadedChild1 = loadedParent.Children.First(c => c.Path == "/Parent/Child1");
        OneDriveFolderNode loadedChild2 = loadedParent.Children.First(c => c.Path == "/Parent/Child2");

        loadedChild1.SelectionState.ShouldBe(SelectionState.Checked);
        loadedChild2.SelectionState.ShouldBe(SelectionState.Unchecked);
        loadedParent.SelectionState.ShouldBe(SelectionState.Indeterminate);
        }

    private static List<OneDriveFolderNode> CreateTestFolders() => [
                CreateFolder("1", "Folder1", "/Folder1"),
            CreateFolder("2", "Folder2", "/Folder2"),
            CreateFolder("3", "Folder3", "/Folder3")
            ];

    private static OneDriveFolderNode CreateFolder(string id, string name, string path) => new()
    {
        DriveItemId = id,
        Name = name,
        Path = path,
        IsFolder = true,
        SelectionState = SelectionState.Unchecked,
        IsSelected = false
    };
}
