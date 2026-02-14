using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit.Services;

public class SyncSelectionServicePersistenceShould
{
    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task SaveCheckedFoldersToDatabase()
    {
        ISyncConfigurationRepository mockRepo = Substitute.For<ISyncConfigurationRepository>();
        var sut = new SyncSelectionService(mockRepo);

        OneDriveFolderNode folder1 = CreateFolder("1", "Folder1", "/Folder1");
        OneDriveFolderNode folder2 = CreateFolder("2", "Folder2", "/Folder2");
        var rootFolders = new List<OneDriveFolderNode> { folder1, folder2 };

        sut.SetSelection(folder1, true);

        await sut.SaveSelectionsToDatabaseAsync("acc-123", rootFolders, TestContext.Current.CancellationToken);

        await mockRepo.Received(1).SaveBatchAsync(
            "acc-123",
            Arg.Is<IEnumerable<FileMetadata>>(configs => configs.Count() == 1 &&
                                                              configs.First().RelativePath == "/Folder1" &&
                                                              configs.First().IsSelected),
            Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task SaveMultipleCheckedFoldersToDatabase()
    {
        ISyncConfigurationRepository mockRepo = Substitute.For<ISyncConfigurationRepository>();
        var sut = new SyncSelectionService(mockRepo);

        OneDriveFolderNode folder1 = CreateFolder("1", "Folder1", "/Folder1");
        OneDriveFolderNode folder2 = CreateFolder("2", "Folder2", "/Folder2");
        var rootFolders = new List<OneDriveFolderNode> { folder1, folder2 };

        sut.SetSelection(folder1, true);
        sut.SetSelection(folder2, true);

        await sut.SaveSelectionsToDatabaseAsync("acc-123", rootFolders, TestContext.Current.CancellationToken);

        await mockRepo.Received(1).SaveBatchAsync(
            "acc-123",
            Arg.Is<IEnumerable<FileMetadata>>(configs => configs.Count() == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task NotSaveUncheckedFoldersToDatabase()
    {
        ISyncConfigurationRepository mockRepo = Substitute.For<ISyncConfigurationRepository>();
        var sut = new SyncSelectionService(mockRepo);

        OneDriveFolderNode folder1 = CreateFolder("1", "Folder1", "/Folder1");
        OneDriveFolderNode folder2 = CreateFolder("2", "Folder2", "/Folder2");
        var rootFolders = new List<OneDriveFolderNode> { folder1, folder2 };

        // Don't select any folders

        await sut.SaveSelectionsToDatabaseAsync("acc-123", rootFolders, TestContext.Current.CancellationToken);

        await mockRepo.Received(1).SaveBatchAsync(
            "acc-123",
            Arg.Any<IEnumerable<FileMetadata>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LoadSelectionsFromDatabaseAndApplyToTree()
    {
        ISyncConfigurationRepository mockRepo = Substitute.For<ISyncConfigurationRepository>();
        _ = mockRepo.GetSelectedFoldersAsync("acc-123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>(["/Folder1"]));

        var sut = new SyncSelectionService(mockRepo);

        OneDriveFolderNode folder1 = CreateFolder("1", "Folder1", "/Folder1");
        OneDriveFolderNode folder2 = CreateFolder("2", "Folder2", "/Folder2");
        var rootFolders = new List<OneDriveFolderNode> { folder1, folder2 };

        await sut.LoadSelectionsFromDatabaseAsync("acc-123", rootFolders, TestContext.Current.CancellationToken);

        folder1.SelectionState.ShouldBe(SelectionState.Checked);
        folder2.SelectionState.ShouldBe(SelectionState.Unchecked);
    }

    [Fact]
    public async Task HandleEmptyDatabaseGracefully()
    {
        ISyncConfigurationRepository mockRepo = Substitute.For<ISyncConfigurationRepository>();
        _ = mockRepo.GetSelectedFoldersAsync("acc-123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([]));

        var sut = new SyncSelectionService(mockRepo);

        OneDriveFolderNode folder = CreateFolder("1", "Folder1", "/Folder1");
        var rootFolders = new List<OneDriveFolderNode> { folder };

        await sut.LoadSelectionsFromDatabaseAsync("acc-123", rootFolders, TestContext.Current.CancellationToken);

        folder.SelectionState.ShouldBe(SelectionState.Unchecked);
    }

    [Fact]
    public async Task IgnoreFoldersInDatabaseThatNoLongerExist()
    {
        ISyncConfigurationRepository mockRepo = Substitute.For<ISyncConfigurationRepository>();
        _ = mockRepo.GetSelectedFoldersAsync("acc-123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([
                "/Folder1",
                "/DeletedFolder",
                "/Folder2"
            ]));

        var sut = new SyncSelectionService(mockRepo);

        OneDriveFolderNode folder1 = CreateFolder("1", "Folder1", "/Folder1");
        OneDriveFolderNode folder2 = CreateFolder("2", "Folder2", "/Folder2");
        var rootFolders = new List<OneDriveFolderNode> { folder1, folder2 };

        await sut.LoadSelectionsFromDatabaseAsync("acc-123", rootFolders, TestContext.Current.CancellationToken);

        folder1.SelectionState.ShouldBe(SelectionState.Checked);
        folder2.SelectionState.ShouldBe(SelectionState.Checked);
        // /DeletedFolder is silently ignored - no exception
    }

    [Fact(Skip = "Requires refactor to support new production code structure")]
    public async Task RecalculateIndeterminateStatesAfterLoading()
    {
        ISyncConfigurationRepository mockRepo = Substitute.For<ISyncConfigurationRepository>();
        _ = mockRepo.GetSelectedFoldersAsync("acc-123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>(["/Parent/Child1"]));

        var sut = new SyncSelectionService(mockRepo);

        OneDriveFolderNode child1 = CreateFolder("c1", "Child1", "/Parent/Child1");
        OneDriveFolderNode child2 = CreateFolder("c2", "Child2", "/Parent/Child2");
        OneDriveFolderNode parent = CreateFolder("p", "Parent", "/Parent");
        parent.Children.Add(child1);
        parent.Children.Add(child2);
        child1.ParentId = "p";
        child2.ParentId = "p";

        var rootFolders = new List<OneDriveFolderNode> { parent };

        await sut.LoadSelectionsFromDatabaseAsync("acc-123", rootFolders, TestContext.Current.CancellationToken);

        child1.SelectionState.ShouldBe(SelectionState.Checked);
        child2.SelectionState.ShouldBe(SelectionState.Unchecked);
        parent.SelectionState.ShouldBe(SelectionState.Indeterminate);
    }

    [Fact]
    public async Task WorkWithNestedFolderStructures()
    {
        ISyncConfigurationRepository mockRepo = Substitute.For<ISyncConfigurationRepository>();
        _ = mockRepo.GetSelectedFoldersAsync("acc-123", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([
                "/Parent/Child/Grandchild"
            ]));

        var sut = new SyncSelectionService(mockRepo);

        OneDriveFolderNode grandchild = CreateFolder("gc", "Grandchild", "/Parent/Child/Grandchild");
        OneDriveFolderNode child = CreateFolder("c", "Child", "/Parent/Child");
        OneDriveFolderNode parent = CreateFolder("p", "Parent", "/Parent");

        child.Children.Add(grandchild);
        parent.Children.Add(child);
        grandchild.ParentId = "c";
        child.ParentId = "p";

        var rootFolders = new List<OneDriveFolderNode> { parent };

        await sut.LoadSelectionsFromDatabaseAsync("acc-123", rootFolders, TestContext.Current.CancellationToken);
        grandchild.SelectionState.ShouldBe(SelectionState.Checked);
        child.SelectionState.ShouldBe(SelectionState.Unchecked);
        parent.SelectionState.ShouldBe(SelectionState.Indeterminate);
    }

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
