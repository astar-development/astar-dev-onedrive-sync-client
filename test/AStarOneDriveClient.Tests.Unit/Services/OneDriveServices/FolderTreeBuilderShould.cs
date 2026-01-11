using AStarOneDriveClient.Models;
using AStarOneDriveClient.Services.OneDriveServices;
using Microsoft.Graph.Models;

namespace AStarOneDriveClient.Tests.Unit.Services.OneDriveServices;

public class FolderTreeBuilderShould
{
    [Fact]
    public void BuildTreeFromFlatList()
    {
        var items = new List<DriveItem>
        {
            CreateFolder("root1", "Root Folder 1", null, "/drive/root:"),
            CreateFolder("root2", "Root Folder 2", null, "/drive/root:"),
            CreateFolder("child1", "Child 1", "root1", "/drive/root:/Root Folder 1"),
            CreateFolder("child2", "Child 2", "root1", "/drive/root:/Root Folder 1")
        };

        List<OneDriveFolderNode> tree = FolderTreeBuilder.BuildTree(items);

        tree.Count.ShouldBe(2);
        tree[0].Name.ShouldBe("Root Folder 1");
        tree[0].Children.Count.ShouldBe(2);
        tree[0].Children[0].Name.ShouldBe("Child 1");
        tree[0].Children[1].Name.ShouldBe("Child 2");
        tree[1].Name.ShouldBe("Root Folder 2");
        tree[1].Children.Count.ShouldBe(0);
    }

    [Fact]
    public void FilterOutFilesAndKeepOnlyFolders()
    {
        var items = new List<DriveItem>
        {
            CreateFolder("folder1", "My Folder", null, "/drive/root:"),
            CreateFile("file1", "My File.txt", null, "/drive/root:")
        };

        List<OneDriveFolderNode> tree = FolderTreeBuilder.BuildTree(items);

        tree.Count.ShouldBe(1);
        tree[0].Name.ShouldBe("My Folder");
        tree[0].IsFolder.ShouldBeTrue();
    }

    [Fact]
    public void SortChildrenAlphabetically()
    {
        var items = new List<DriveItem>
        {
            CreateFolder("root", "Root", null, "/drive/root:"),
            CreateFolder("childZ", "Zebra", "root", "/drive/root:/Root"),
            CreateFolder("childA", "Apple", "root", "/drive/root:/Root"),
            CreateFolder("childM", "Mango", "root", "/drive/root:/Root")
        };

        List<OneDriveFolderNode> tree = FolderTreeBuilder.BuildTree(items);

        tree.Count.ShouldBe(1);
        tree[0].Children.Count.ShouldBe(3);
        tree[0].Children[0].Name.ShouldBe("Apple");
        tree[0].Children[1].Name.ShouldBe("Mango");
        tree[0].Children[2].Name.ShouldBe("Zebra");
    }

    [Fact]
    public void BuildDeepHierarchy()
    {
        var items = new List<DriveItem>
        {
            CreateFolder("level1", "Level 1", null, "/drive/root:"),
            CreateFolder("level2", "Level 2", "level1", "/drive/root:/Level 1"),
            CreateFolder("level3", "Level 3", "level2", "/drive/root:/Level 1/Level 2")
        };

        List<OneDriveFolderNode> tree = FolderTreeBuilder.BuildTree(items);

        tree.Count.ShouldBe(1);
        tree[0].Name.ShouldBe("Level 1");
        tree[0].Children.Count.ShouldBe(1);
        tree[0].Children[0].Name.ShouldBe("Level 2");
        tree[0].Children[0].Children.Count.ShouldBe(1);
        tree[0].Children[0].Children[0].Name.ShouldBe("Level 3");
    }

    [Fact]
    public void BuildCorrectPaths()
    {
        var items = new List<DriveItem>
        {
            CreateFolder("docs", "Documents", null, "/drive/root:"),
            CreateFolder("work", "Work", "docs", "/drive/root:/Documents")
        };

        List<OneDriveFolderNode> tree = FolderTreeBuilder.BuildTree(items);

        tree[0].Path.ShouldBe("/Documents");
        tree[0].Children[0].Path.ShouldBe("/Documents/Work");
    }

    [Fact]
    public void HandleEmptyList()
    {
        var items = new List<DriveItem>();

        List<OneDriveFolderNode> tree = FolderTreeBuilder.BuildTree(items);

        tree.ShouldBeEmpty();
    }

    [Fact]
    public void HandleItemsWithoutParent()
    {
        var items = new List<DriveItem>
        {
            CreateFolder("orphan", "Orphan Folder", "nonexistent", "/drive/root:/Somewhere")
        };

        List<OneDriveFolderNode> tree = FolderTreeBuilder.BuildTree(items);

        tree.ShouldBeEmpty();
    }

    [Fact]
    public void BuildTreeWithSpecificRootParentId()
    {
        var items = new List<DriveItem>
        {
            CreateFolder("root", "Root", null, "/drive/root:"),
            CreateFolder("child1", "Child 1", "root", "/drive/root:/Root"),
            CreateFolder("child2", "Child 2", "root", "/drive/root:/Root"),
            CreateFolder("grandchild", "Grandchild", "child1", "/drive/root:/Root/Child 1")
        };

        List<OneDriveFolderNode> tree = FolderTreeBuilder.BuildTree(items, "root");

        tree.Count.ShouldBe(2);
        tree[0].Name.ShouldBe("Child 1");
        tree[1].Name.ShouldBe("Child 2");
        tree[0].Children.Count.ShouldBe(1);
        tree[0].Children[0].Name.ShouldBe("Grandchild");
    }

    [Fact]
    public void MergeNewItemsIntoExistingTree()
    {
        var existingTree = new List<OneDriveFolderNode>
        {
            new()
            {
                Id = "parent",
                Name = "Parent",
                Path = "/Parent",
                IsFolder = true
            }
        };

        var newItems = new List<DriveItem>
        {
            CreateFolder("child1", "Child 1", "parent", "/drive/root:/Parent"),
            CreateFolder("child2", "Child 2", "parent", "/drive/root:/Parent")
        };

        FolderTreeBuilder.MergeIntoTree(existingTree, newItems, "parent");

        existingTree[0].Children.Count.ShouldBe(2);
        existingTree[0].Children[0].Name.ShouldBe("Child 1");
        existingTree[0].Children[1].Name.ShouldBe("Child 2");
        existingTree[0].ChildrenLoaded.ShouldBeTrue();
    }

    [Fact]
    public void MergeDoesNotAddDuplicates()
    {
        var existingTree = new List<OneDriveFolderNode>
        {
            new()
            {
                Id = "parent",
                Name = "Parent",
                Path = "/Parent",
                IsFolder = true,
                Children =
                {
                    new OneDriveFolderNode
                    {
                        Id = "child1",
                        Name = "Child 1",
                        Path = "/Parent/Child 1",
                        ParentId = "parent",
                        IsFolder = true
                    }
                }
            }
        };

        var newItems = new List<DriveItem>
        {
            CreateFolder("child1", "Child 1", "parent", "/drive/root:/Parent"),
            CreateFolder("child2", "Child 2", "parent", "/drive/root:/Parent")
        };

        FolderTreeBuilder.MergeIntoTree(existingTree, newItems, "parent");

        existingTree[0].Children.Count.ShouldBe(2);
        existingTree[0].Children.Count(c => c.Id == "child1").ShouldBe(1);
    }

    [Fact]
    public void HandleItemsWithNullId()
    {
        var items = new List<DriveItem>
        {
            new()
            {
                Name = "No ID Folder",
                Folder = new Folder(),
                ParentReference = new ItemReference { Id = null, Path = "/drive/root:" }
            },
            CreateFolder("valid", "Valid Folder", null, "/drive/root:")
        };

        List<OneDriveFolderNode> tree = FolderTreeBuilder.BuildTree(items);

        tree.Count.ShouldBe(1);
        tree[0].Id.ShouldBe("valid");
    }

    [Fact]
    public void HandleItemsWithNullName()
    {
        var items = new List<DriveItem>
        {
            new()
            {
                Id = "noname",
                Name = null,
                Folder = new Folder(),
                ParentReference = new ItemReference { Id = null, Path = "/drive/root:" }
            },
            CreateFolder("valid", "Valid Folder", null, "/drive/root:")
        };

        List<OneDriveFolderNode> tree = FolderTreeBuilder.BuildTree(items);

        tree.Count.ShouldBe(1);
        tree[0].Id.ShouldBe("valid");
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenItemsIsNull()
    {
        Exception? exception = Record.Exception(() => FolderTreeBuilder.BuildTree(null!));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenMergeTreeIsNull()
    {
        var items = new List<DriveItem> { CreateFolder("test", "Test", "parent", "/drive/root:") };

        Exception? exception = Record.Exception(() => FolderTreeBuilder.MergeIntoTree(null!, items, "parent"));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenMergeItemsIsNull()
    {
        var tree = new List<OneDriveFolderNode>();

        Exception? exception = Record.Exception(() => FolderTreeBuilder.MergeIntoTree(tree, null!, "parent"));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenMergeParentIdIsNull()
    {
        var tree = new List<OneDriveFolderNode>();
        var items = new List<DriveItem>();

        Exception? exception = Record.Exception(() => FolderTreeBuilder.MergeIntoTree(tree, items, null!));

        _ = exception.ShouldNotBeNull();
        _ = exception.ShouldBeOfType<ArgumentNullException>();
    }

    private static DriveItem CreateFolder(string id, string name, string? parentId, string parentPath) => new()
    {
        Id = id,
        Name = name,
        Folder = new Folder(),
        ParentReference = new ItemReference
        {
            Id = parentId,
            Path = parentPath
        }
    };

    private static DriveItem CreateFile(string id, string name, string? parentId, string parentPath) => new()
    {
        Id = id,
        Name = name,
        ParentReference = new ItemReference
        {
            Id = parentId,
            Path = parentPath
        }
    };
}
