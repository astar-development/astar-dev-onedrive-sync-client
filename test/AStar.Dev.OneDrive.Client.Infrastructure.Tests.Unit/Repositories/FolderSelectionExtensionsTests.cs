using System;
using System.Collections.Generic;
using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Client.Tests.Infrastructure.Repositories;

public class FolderSelectionExtensionsTests
{
    // ------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------

    private static DriveItemEntity Item(string path, bool isFolder = true)
        => new(
            accountId: "acc",
            driveItemId: Guid.NewGuid().ToString(),
            relativePath: path,
            eTag: null,
            cTag: null,
            size: 0,
            lastModifiedUtc: DateTimeOffset.UtcNow,
            isFolder: isFolder,
            isDeleted: false,
            isSelected: false,
            remoteHash: null,
            name: System.IO.Path.GetFileName(path),
            localPath: null,
            localHash: null,
            syncStatus: FileSyncStatus.SyncOnly,
            lastSyncDirection: SyncDirection.None
        );

    private static FileMetadata Meta(string path, bool? selected)
        => new(
            DriveItemId: Guid.NewGuid().ToString(),
            AccountId: "acc",
            Name: System.IO.Path.GetFileName(path),
            RelativePath: path,
            Size: 0,
            LastModifiedUtc: DateTimeOffset.UtcNow,
            LocalPath: "",
            IsFolder: true,
            IsDeleted: false,
            IsSelected: selected ?? false
        );

    // ------------------------------------------------------------
    // 1. Normalization
    // ------------------------------------------------------------

    [Fact]
    public void Normalization_Should_Resolve_Equivalent_Paths()
    {
        DriveItemEntity[] items =
        [
            Item("A/B/C"),
            Item("\\A\\B\\C\\"),
            Item("/A/B/C/")
        ];

        FileMetadata[] meta =
        [
            Meta("/A/B/C", true)
        ];

        items.ApplyHierarchicalSelection(meta);

        foreach(DriveItemEntity? item in items)
            item.IsSelected.ShouldBe(true);
    }

    // ------------------------------------------------------------
    // 2. Direct match
    // ------------------------------------------------------------

    [Fact]
    public void DirectMatch_Should_Apply_Selection()
    {
        DriveItemEntity[] items =
        [
            Item("/A/B")
        ];

        FileMetadata[] meta =
        [
            Meta("/A/B", true)
        ];

        items.ApplyHierarchicalSelection(meta);

        items[0].IsSelected.ShouldBe(true);
    }

    // ------------------------------------------------------------
    // 3. Ancestor match
    // ------------------------------------------------------------

    [Fact]
    public void AncestorMatch_Should_Propagate_Selection()
    {
        DriveItemEntity[] items =
        [
            Item("/A/B/C/D")
        ];

        FileMetadata[] meta =
        [
            Meta("/A/B", true)
        ];

        items.ApplyHierarchicalSelection(meta);

        items[0].IsSelected.ShouldBe(true);
    }

    // ------------------------------------------------------------
    // 4. Tri-state: explicit null
    // ------------------------------------------------------------

    [Fact]
    public void ExplicitNull_Should_Apply_Null()
    {
        DriveItemEntity[] items =
        [
            Item("/A/B/C")
        ];

        FileMetadata[] meta =
        [
            Meta("/A/B/C", null)
        ];

        items.ApplyHierarchicalSelection(meta);

        items[0].IsSelected.ShouldBeNull();
    }

    // ------------------------------------------------------------
    // 5. Tri-state: inherited null
    // ------------------------------------------------------------

    [Fact]
    public void InheritedNull_Should_Apply_Null()
    {
        DriveItemEntity[] items =
        [
            Item("/A/B/C/D")
        ];

        FileMetadata[] meta =
        [
            Meta("/A/B", null)
        ];

        items.ApplyHierarchicalSelection(meta);

        items[0].IsSelected.ShouldBeNull();
    }

    // ------------------------------------------------------------
    // 6. Tri-state: false overrides ancestor true
    // ------------------------------------------------------------

    [Fact]
    public void ExplicitFalse_Should_Override_Ancestor_True()
    {
        DriveItemEntity[] items =
        [
            Item("/A/B/C/D")
        ];

        FileMetadata[] meta =
        [
            Meta("/A", true),
            Meta("/A/B/C", false)
        ];

        items.ApplyHierarchicalSelection(meta);

        items[0].IsSelected.ShouldBe(false);
    }

    // ------------------------------------------------------------
    // 7. File inherits from folder
    // ------------------------------------------------------------

    [Fact]
    public void File_Should_Inherit_Selection_From_ParentFolder()
    {
        DriveItemEntity[] items =
        [
            Item("/A/B/C/file.txt", isFolder: false)
        ];

        FileMetadata[] meta =
        [
            Meta("/A/B", true)
        ];

        items.ApplyHierarchicalSelection(meta);

        items[0].IsSelected.ShouldBe(true);
    }

    // ------------------------------------------------------------
    // 8. File inherits null
    // ------------------------------------------------------------

    [Fact]
    public void File_Should_Inherit_Null_When_Parent_Is_Null()
    {
        DriveItemEntity[] items =
        [
            Item("/A/B/C/file.txt", isFolder: false)
        ];

        FileMetadata[] meta =
        [
            Meta("/A/B", null)
        ];

        items.ApplyHierarchicalSelection(meta);

        items[0].IsSelected.ShouldBeNull();
    }

    // ------------------------------------------------------------
    // 9. No metadata → all null
    // ------------------------------------------------------------

    [Fact]
    public void NoMetadata_Should_Leave_All_Items_Null()
    {
        DriveItemEntity[] items =
        [
            Item("/A"),
            Item("/A/B"),
            Item("/A/B/C/file.txt", isFolder: false)
        ];

        FileMetadata[] meta = Array.Empty<FileMetadata>();

        items.ApplyHierarchicalSelection(meta);

        foreach(DriveItemEntity? item in items)
            item.IsSelected.ShouldBeNull();
    }

    // ------------------------------------------------------------
    // 10. Root metadata applies everywhere
    // ------------------------------------------------------------

    [Fact]
    public void RootSelection_Should_Apply_To_All_Items()
    {
        DriveItemEntity[] items =
        [
            Item("/"),
            Item("/A"),
            Item("/A/B"),
            Item("/A/B/file.txt", isFolder: false)
        ];

        FileMetadata[] meta =
        [
            Meta("/", true)
        ];

        items.ApplyHierarchicalSelection(meta);

        foreach(DriveItemEntity? item in items)
            item.IsSelected.ShouldBe(true);
    }

    // ------------------------------------------------------------
    // 11. Mixed folder/file scenario
    // ------------------------------------------------------------

    [Fact]
    public void MixedHierarchy_Should_Apply_Correct_Selections()
    {
        DriveItemEntity[] items =
        [
            Item("/A"),
            Item("/A/B"),
            Item("/A/B/C"),
            Item("/A/B/C/file1.txt", isFolder: false),
            Item("/A/B/D"),
            Item("/A/B/D/file2.txt", isFolder: false)
        ];

        FileMetadata[] meta =
        [
            Meta("/A/B", true),
            Meta("/A/B/D", false)
        ];

        items.ApplyHierarchicalSelection(meta);

        items[0].IsSelected.ShouldBeNull();   // /A
        items[1].IsSelected.ShouldBe(true);   // /A/B
        items[2].IsSelected.ShouldBe(true);   // /A/B/C
        items[3].IsSelected.ShouldBe(true);   // file1.txt
        items[4].IsSelected.ShouldBe(false);  // /A/B/D
        items[5].IsSelected.ShouldBe(false);  // file2.txt
    }
}
