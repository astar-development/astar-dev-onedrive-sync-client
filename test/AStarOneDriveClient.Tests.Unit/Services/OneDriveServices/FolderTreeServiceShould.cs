using AStarOneDriveClient.Authentication;
using AStarOneDriveClient.Services.OneDriveServices;
using Microsoft.Graph.Models;

namespace AStarOneDriveClient.Tests.Unit.Services.OneDriveServices;

public class FolderTreeServiceShould
{
    [Fact]
    public async Task ReturnEmptyListWhenAccountIsNotAuthenticated()
    {
        var mockGraph = Substitute.For<IGraphApiClient>();
        var mockAuth = Substitute.For<IAuthService>();
        mockAuth.IsAuthenticatedAsync("account1", Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        var service = new FolderTreeService(mockGraph, mockAuth);

        var result = await service.GetRootFoldersAsync("account1", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
        await mockGraph.DidNotReceive().GetRootChildrenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReturnRootFoldersWhenAuthenticated()
    {
        var mockGraph = Substitute.For<IGraphApiClient>();
        var mockAuth = Substitute.For<IAuthService>();
        mockAuth.IsAuthenticatedAsync("account1", Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var driveItems = new List<DriveItem>
        {
            new() { Id = "folder1", Name = "Documents", Folder = new Folder() },
            new() { Id = "folder2", Name = "Pictures", Folder = new Folder() },
            new() { Id = "file1", Name = "File.txt" } // Files don't have Folder property
        };
        mockGraph.GetRootChildrenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<DriveItem>>(driveItems));

        var service = new FolderTreeService(mockGraph, mockAuth);

        var result = await service.GetRootFoldersAsync("account1", TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe("folder1");
        result[0].Name.ShouldBe("Documents");
        result[0].Path.ShouldBe("/Documents");
        result[0].ParentId.ShouldBeNull();
        result[0].IsFolder.ShouldBeTrue();
        result[1].Name.ShouldBe("Pictures");
    }

    [Fact]
    public async Task FilterOutFilesAndReturnOnlyFolders()
    {
        var mockGraph = Substitute.For<IGraphApiClient>();
        var mockAuth = Substitute.For<IAuthService>();
        mockAuth.IsAuthenticatedAsync("account1", Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var driveItems = new List<DriveItem>
        {
            new() { Id = "folder1", Name = "Documents", Folder = new Folder() },
            new() { Id = "file1", Name = "File1.txt" },
            new() { Id = "file2", Name = "File2.docx" }
        };
        mockGraph.GetRootChildrenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<DriveItem>>(driveItems));

        var service = new FolderTreeService(mockGraph, mockAuth);

        var result = await service.GetRootFoldersAsync("account1", TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Documents");
    }

    [Fact]
    public async Task GetChildFoldersForSpecificParent()
    {
        var mockGraph = Substitute.For<IGraphApiClient>();
        var mockAuth = Substitute.For<IAuthService>();
        mockAuth.IsAuthenticatedAsync("account1", Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var parentItem = new DriveItem
        {
            Id = "parent1",
            Name = "Documents",
            ParentReference = new ItemReference { Path = "/drive/root:" }
        };
        mockGraph.GetDriveItemAsync(Arg.Any<string>(), "parent1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<DriveItem?>(parentItem));

        var childItems = new List<DriveItem>
        {
            new() { Id = "child1", Name = "Work", Folder = new Folder() },
            new() { Id = "child2", Name = "Personal", Folder = new Folder() }
        };
        mockGraph.GetDriveItemChildrenAsync(Arg.Any<string>(), "parent1", Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<DriveItem>>(childItems));

        var service = new FolderTreeService(mockGraph, mockAuth);

        var result = await service.GetChildFoldersAsync("account1", "parent1", TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result[0].Id.ShouldBe("child1");
        result[0].Name.ShouldBe("Work");
        result[0].ParentId.ShouldBe("parent1");
        result[0].Path.ShouldBe("/drive/root:/Documents/Work");
    }

    [Fact]
    public async Task ReturnEmptyListWhenGettingChildrenForUnauthenticatedAccount()
    {
        var mockGraph = Substitute.For<IGraphApiClient>();
        var mockAuth = Substitute.For<IAuthService>();
        mockAuth.IsAuthenticatedAsync("account1", Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        var service = new FolderTreeService(mockGraph, mockAuth);

        var result = await service.GetChildFoldersAsync("account1", "parent1", TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
        await mockGraph.DidNotReceive().GetDriveItemChildrenAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetFolderHierarchyReturnsEmptyWhenNotAuthenticated()
    {
        var mockGraph = Substitute.For<IGraphApiClient>();
        var mockAuth = Substitute.For<IAuthService>();
        mockAuth.IsAuthenticatedAsync("account1", Arg.Any<CancellationToken>()).Returns(Task.FromResult(false));

        var service = new FolderTreeService(mockGraph, mockAuth);

        var result = await service.GetFolderHierarchyAsync("account1", maxDepth: 1, cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetFolderHierarchyCallsGetRootFoldersAsync()
    {
        var mockGraph = Substitute.For<IGraphApiClient>();
        var mockAuth = Substitute.For<IAuthService>();
        mockAuth.IsAuthenticatedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(true));

        var rootItems = new List<DriveItem>
        {
            new() { Id = "root1", Name = "Documents", Folder = new Folder() }
        };
        mockGraph.GetRootChildrenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IEnumerable<DriveItem>>(rootItems));

        var service = new FolderTreeService(mockGraph, mockAuth);

        var result = await service.GetFolderHierarchyAsync("account1", maxDepth: 0, cancellationToken: TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Documents");
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionWhenAccountIdIsNull()
    {
        var mockGraph = Substitute.For<IGraphApiClient>();
        var mockAuth = Substitute.For<IAuthService>();
        var service = new FolderTreeService(mockGraph, mockAuth);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await service.GetRootFoldersAsync(null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionWhenParentFolderIdIsNull()
    {
        var mockGraph = Substitute.For<IGraphApiClient>();
        var mockAuth = Substitute.For<IAuthService>();
        var service = new FolderTreeService(mockGraph, mockAuth);

        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await service.GetChildFoldersAsync("account1", null!, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenGraphApiClientIsNull()
    {
        var mockAuth = Substitute.For<IAuthService>();

        Should.Throw<ArgumentNullException>(() =>
            new FolderTreeService(null!, mockAuth));
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenAuthServiceIsNull()
    {
        var mockGraph = Substitute.For<IGraphApiClient>();

        Should.Throw<ArgumentNullException>(() =>
            new FolderTreeService(mockGraph, null!));
    }
}
