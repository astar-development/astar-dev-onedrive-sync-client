using System.Reactive.Subjects;
using AStarOneDriveClient.Data;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.Services.OneDriveServices;
using AStarOneDriveClient.ViewModels;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;

namespace AStarOneDriveClient.Tests.Unit.ViewModels;

/// <summary>
/// Integration tests for folder selection persistence through the full stack.
/// </summary>
public class SyncTreeViewModelPersistenceIntegrationShould : IDisposable
{
    private readonly SyncDbContext _context;
    private readonly SyncConfigurationRepository _configRepository;
    private readonly SyncSelectionService _selectionService;
    private readonly IFolderTreeService _mockFolderTreeService;
    private readonly ISyncEngine _mockSyncEngine;
    private readonly Subject<SyncState> _progressSubject;

    public SyncTreeViewModelPersistenceIntegrationShould()
    {
        var options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        _context = new SyncDbContext(options);
        _configRepository = new SyncConfigurationRepository(_context);
        _selectionService = new SyncSelectionService(_configRepository);
        _mockFolderTreeService = Substitute.For<IFolderTreeService>();
        _mockSyncEngine = Substitute.For<ISyncEngine>();

        _progressSubject = new Subject<SyncState>();
        _mockSyncEngine.Progress.Returns(_progressSubject);
    }

    [Fact]
    public async Task PersistSelectionsToDatabase()
    {
        // Arrange
        var folders = CreateTestFolders();
        _mockFolderTreeService.GetRootFoldersAsync("acc-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>(folders));

        using var sut = new SyncTreeViewModel(_mockFolderTreeService, _selectionService, _mockSyncEngine);
        sut.SelectedAccountId = "acc-1";
        await Task.Delay(100); // Allow async load

        // Act - Select a folder
        var folderToSelect = sut.RootFolders.First();
        sut.ToggleSelectionCommand.Execute(folderToSelect).Subscribe();
        await Task.Delay(100); // Allow async save

        // Assert - Check database
        var savedPaths = await _configRepository.GetSelectedFoldersAsync("acc-1");
        savedPaths.ShouldContain("/Folder1");
    }

    [Fact]
    public async Task RestoreSelectionsFromDatabase()
    {
        // Arrange - Pre-populate database
        await _configRepository.SaveBatchAsync("acc-1", [
            new SyncConfiguration(0, "acc-1", "/Folder2", true, DateTime.UtcNow)
        ]);

        var folders = CreateTestFolders();
        _mockFolderTreeService.GetRootFoldersAsync("acc-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>(folders));

        // Act - Load folders (should restore selections)
        using var sut = new SyncTreeViewModel(_mockFolderTreeService, _selectionService, _mockSyncEngine);
        sut.SelectedAccountId = "acc-1";
        await Task.Delay(150);

        // Assert
        var folder2 = sut.RootFolders.FirstOrDefault(f => f.Path == "/Folder2");
        folder2.ShouldNotBeNull();
        folder2.SelectionState.ShouldBe(SelectionState.Checked);
    }

    [Fact]
    public async Task ClearSelectionsFromDatabase()
    {
        // Arrange - Pre-populate database
        await _configRepository.SaveBatchAsync("acc-1", [
            new SyncConfiguration(0, "acc-1", "/Folder1", true, DateTime.UtcNow)
        ]);

        var folders = CreateTestFolders();
        _mockFolderTreeService.GetRootFoldersAsync("acc-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>(folders));

        using var sut = new SyncTreeViewModel(_mockFolderTreeService, _selectionService, _mockSyncEngine);
        sut.SelectedAccountId = "acc-1";
        await Task.Delay(150);

        // Act - Clear all selections
        sut.ClearSelectionsCommand.Execute().Subscribe();
        await Task.Delay(100); // Allow async save

        // Assert - Check database
        var savedPaths = await _configRepository.GetSelectedFoldersAsync("acc-1");
        savedPaths.ShouldBeEmpty();
    }

    [Fact]
    public async Task MaintainSeparateSelectionsPerAccount()
    {
        // Arrange - Create selections for two accounts
        await _configRepository.SaveBatchAsync("acc-1", [
            new SyncConfiguration(0, "acc-1", "/Folder1", true, DateTime.UtcNow)
        ]);
        await _configRepository.SaveBatchAsync("acc-2", [
            new SyncConfiguration(0, "acc-2", "/Folder2", true, DateTime.UtcNow)
        ]);

        var folders = CreateTestFolders();
        _mockFolderTreeService.GetRootFoldersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>(folders));

        // Act - Load account 1
        using var sut = new SyncTreeViewModel(_mockFolderTreeService, _selectionService, _mockSyncEngine);
        sut.SelectedAccountId = "acc-1";
        await Task.Delay(150);

        var folder1Selected = sut.RootFolders.First(f => f.Path == "/Folder1").SelectionState;
        var folder2Selected = sut.RootFolders.First(f => f.Path == "/Folder2").SelectionState;

        // Assert
        folder1Selected.ShouldBe(SelectionState.Checked);
        folder2Selected.ShouldBe(SelectionState.Unchecked);
    }

    [Fact]
    public async Task HandleDatabaseErrorsGracefully()
    {
        // Arrange - Dispose context to cause errors
        _context.Dispose();

        var folders = CreateTestFolders();
        _mockFolderTreeService.GetRootFoldersAsync("acc-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>(folders));

        // Act - Should not throw even if database is unavailable
        using var sut = new SyncTreeViewModel(_mockFolderTreeService, _selectionService, _mockSyncEngine);
        sut.SelectedAccountId = "acc-1";
        await Task.Delay(150);

        var folderToSelect = sut.RootFolders.First();
        Should.NotThrow(() => sut.ToggleSelectionCommand.Execute(folderToSelect).Subscribe());
        await Task.Delay(100);

        // Assert - UI still functional
        sut.RootFolders.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task RestoreIndeterminateStatesCorrectly()
    {
        // Arrange - Save only one child checked
        await _configRepository.SaveBatchAsync("acc-1", [
            new SyncConfiguration(0, "acc-1", "/Parent/Child1", true, DateTime.UtcNow)
        ]);

        var child1 = CreateFolder("c1", "Child1", "/Parent/Child1");
        var child2 = CreateFolder("c2", "Child2", "/Parent/Child2");
        var parent = CreateFolder("p", "Parent", "/Parent");
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        child1.ParentId = "p";
        child2.ParentId = "p";

        _mockFolderTreeService.GetRootFoldersAsync("acc-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<OneDriveFolderNode>>([parent]));

        // Act
        using var sut = new SyncTreeViewModel(_mockFolderTreeService, _selectionService, _mockSyncEngine);
        sut.SelectedAccountId = "acc-1";
        await Task.Delay(150);

        // Assert
        var loadedParent = sut.RootFolders[0];
        var loadedChild1 = loadedParent.Children.First(c => c.Path == "/Parent/Child1");
        var loadedChild2 = loadedParent.Children.First(c => c.Path == "/Parent/Child2");

        loadedChild1.SelectionState.ShouldBe(SelectionState.Checked);
        loadedChild2.SelectionState.ShouldBe(SelectionState.Unchecked);
        loadedParent.SelectionState.ShouldBe(SelectionState.Indeterminate);
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    private static List<OneDriveFolderNode> CreateTestFolders() =>
    [
        CreateFolder("1", "Folder1", "/Folder1"),
        CreateFolder("2", "Folder2", "/Folder2"),
        CreateFolder("3", "Folder3", "/Folder3")
    ];

    private static OneDriveFolderNode CreateFolder(string id, string name, string path) =>
        new()
        {
            Id = id,
            Name = name,
            Path = path,
            IsFolder = true,
            SelectionState = SelectionState.Unchecked,
            IsSelected = false
        };
}
