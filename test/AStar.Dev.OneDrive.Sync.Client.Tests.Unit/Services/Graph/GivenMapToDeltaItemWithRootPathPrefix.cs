using System.Reflection;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using Microsoft.Graph.Models;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMockRequest = WireMock.RequestBuilders.Request;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Graph;

public sealed class GivenMapToDeltaItemWithRootPathPrefix
{
    private static readonly MethodInfo MapToDeltaItem =
        typeof(GraphService).GetMethod("MapToDeltaItem", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static DeltaItem Invoke(DriveItem item)
        => (DeltaItem)MapToDeltaItem.Invoke(null, [item])!;

    [Fact]
    public void when_parent_path_contains_root_marker_then_relative_path_uses_after_root_prefix()
    {
        var item = new DriveItem
        {
            Id = "item-1",
            Name = "report.docx",
            ParentReference = new ItemReference
            {
                Path = "/drive/root:/Documents",
                DriveId = "drive-123",
                Id = "folder-456"
            }
        };

        DeltaItem result = Invoke(item);

        result.RelativePath.ShouldBe("Documents/report.docx");
    }

    [Fact]
    public void when_parent_path_contains_root_marker_with_nested_path_then_full_prefix_is_used()
    {
        var item = new DriveItem
        {
            Id = "item-2",
            Name = "photo.jpg",
            ParentReference = new ItemReference
            {
                Path = "/drive/root:/Pictures/2024",
                DriveId = "drive-123",
                Id = "folder-789"
            }
        };

        DeltaItem result = Invoke(item);

        result.RelativePath.ShouldBe("Pictures/2024/photo.jpg");
    }

    [Fact]
    public void when_parent_path_does_not_contain_root_marker_then_relative_path_is_item_name_only()
    {
        var item = new DriveItem
        {
            Id = "item-3",
            Name = "readme.txt",
            ParentReference = new ItemReference
            {
                Path = "/drives/b!abc/items/xyz",
                DriveId = "drive-123",
                Id = "folder-root"
            }
        };

        DeltaItem result = Invoke(item);

        result.RelativePath.ShouldBe("readme.txt");
    }

    [Fact]
    public void when_parent_path_is_empty_then_relative_path_is_item_name()
    {
        var item = new DriveItem
        {
            Id = "item-4",
            Name = "standalone.txt",
            ParentReference = new ItemReference
            {
                Path = string.Empty,
                DriveId = "drive-123",
                Id = "parent-id"
            }
        };

        DeltaItem result = Invoke(item);

        result.RelativePath.ShouldBe("standalone.txt");
    }

    [Fact]
    public void when_parent_reference_is_null_then_relative_path_is_item_name()
    {
        var item = new DriveItem
        {
            Id = "item-5",
            Name = "orphan.txt",
            ParentReference = null
        };

        DeltaItem result = Invoke(item);

        result.RelativePath.ShouldBe("orphan.txt");
    }
}

public sealed class GivenMapToDeltaItemDeletionAndFolderFlags
{
    private static readonly MethodInfo MapToDeltaItem =
        typeof(GraphService).GetMethod("MapToDeltaItem", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static DeltaItem Invoke(DriveItem item)
        => (DeltaItem)MapToDeltaItem.Invoke(null, [item])!;

    [Fact]
    public void when_item_deleted_is_not_null_then_is_deleted_is_true()
    {
        var item = new DriveItem
        {
            Id = "item-del",
            Name = "gone.txt",
            Deleted = new Deleted(),
            ParentReference = new ItemReference { DriveId = "drv", Id = "par" }
        };

        DeltaItem result = Invoke(item);

        result.IsDeleted.ShouldBeTrue();
    }

    [Fact]
    public void when_item_deleted_is_null_then_is_deleted_is_false()
    {
        var item = new DriveItem
        {
            Id = "item-alive",
            Name = "alive.txt",
            Deleted = null,
            ParentReference = new ItemReference { DriveId = "drv", Id = "par" }
        };

        DeltaItem result = Invoke(item);

        result.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public void when_item_folder_is_not_null_then_is_folder_is_true()
    {
        var item = new DriveItem
        {
            Id = "folder-id",
            Name = "MyFolder",
            Folder = new Folder(),
            ParentReference = new ItemReference { DriveId = "drv", Id = "par" }
        };

        DeltaItem result = Invoke(item);

        result.IsFolder.ShouldBeTrue();
    }

    [Fact]
    public void when_item_folder_is_null_then_is_folder_is_false()
    {
        var item = new DriveItem
        {
            Id = "file-id",
            Name = "data.csv",
            Folder = null,
            ParentReference = new ItemReference { DriveId = "drv", Id = "par" }
        };

        DeltaItem result = Invoke(item);

        result.IsFolder.ShouldBeFalse();
    }
}

public sealed class GivenMapToDeltaItemDownloadUrl
{
    private static readonly MethodInfo MapToDeltaItem =
        typeof(GraphService).GetMethod("MapToDeltaItem", BindingFlags.NonPublic | BindingFlags.Static)!;

    private static DeltaItem Invoke(DriveItem item)
        => (DeltaItem)MapToDeltaItem.Invoke(null, [item])!;

    [Fact]
    public void when_additional_data_contains_download_url_then_download_url_is_populated()
    {
        const string expectedUrl = "https://cdn.example.com/file.bin?token=xyz";
        var item = new DriveItem
        {
            Id = "item-dl",
            Name = "archive.zip",
            ParentReference = new ItemReference { DriveId = "drv", Id = "par" },
            AdditionalData = new Dictionary<string, object>
            {
                ["@microsoft.graph.downloadUrl"] = expectedUrl
            }
        };

        DeltaItem result = Invoke(item);

        result.DownloadUrl.ShouldBe(expectedUrl);
    }

    [Fact]
    public void when_additional_data_does_not_contain_download_url_then_download_url_is_null()
    {
        var item = new DriveItem
        {
            Id = "item-no-dl",
            Name = "folder-item",
            Folder = new Folder(),
            ParentReference = new ItemReference { DriveId = "drv", Id = "par" }
        };

        DeltaItem result = Invoke(item);

        result.DownloadUrl.ShouldBeNull();
    }
}

public sealed class GivenAGraphServiceWithWireMock : IDisposable
{
    private const string AccessToken = "test-access-token";
    private const string DriveId = "drive-abc";
    private const string RootId = "root-xyz";

    private readonly WireMockServer _server;

    public GivenAGraphServiceWithWireMock() => _server = WireMockServer.Start();

    public void Dispose() => _server.Stop();

    [Fact]
    public async Task when_get_drive_id_is_called_then_returns_drive_id()
    {
        GraphService service = CreateService();
        StubMeDrive(DriveId);
        StubDriveRoot(DriveId, RootId);

        var result = await service.GetDriveIdAsync(AccessToken, TestContext.Current.CancellationToken);

        result.ShouldBe(DriveId);
    }

    [Fact]
    public async Task when_get_root_folders_is_called_then_files_are_excluded()
    {
        GraphService service = CreateService();
        StubMeDrive(DriveId);
        StubDriveRoot(DriveId, RootId);
        _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + DriveId + "/items/" + RootId + "/children").UsingGet())
               .RespondWith(JsonResponse(
                   "{\"value\":[" +
                   "{\"id\":\"f1\",\"name\":\"Alpha\",\"folder\":{},\"parentReference\":{\"id\":\"" + RootId + "\",\"driveId\":\"" + DriveId + "\"}}," +
                   "{\"id\":\"f2\",\"name\":\"Zebra\",\"folder\":{},\"parentReference\":{\"id\":\"" + RootId + "\",\"driveId\":\"" + DriveId + "\"}}," +
                   "{\"id\":\"file1\",\"name\":\"readme.txt\",\"file\":{},\"parentReference\":{\"id\":\"" + RootId + "\",\"driveId\":\"" + DriveId + "\"}}" +
                   "]}"));

        List<DriveFolder> result = await service.GetRootFoldersAsync(AccessToken, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result.ShouldAllBe(folder => folder.Id.StartsWith("f"));
    }

    [Fact]
    public async Task when_get_root_folders_is_called_then_results_are_sorted_by_name()
    {
        GraphService service = CreateService();
        StubMeDrive(DriveId);
        StubDriveRoot(DriveId, RootId);
        _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + DriveId + "/items/" + RootId + "/children").UsingGet())
               .RespondWith(JsonResponse(
                   "{\"value\":[" +
                   "{\"id\":\"fz\",\"name\":\"Zebra\",\"folder\":{},\"parentReference\":{\"id\":\"" + RootId + "\",\"driveId\":\"" + DriveId + "\"}}," +
                   "{\"id\":\"fa\",\"name\":\"Alpha\",\"folder\":{},\"parentReference\":{\"id\":\"" + RootId + "\",\"driveId\":\"" + DriveId + "\"}}," +
                   "{\"id\":\"fm\",\"name\":\"Mango\",\"folder\":{},\"parentReference\":{\"id\":\"" + RootId + "\",\"driveId\":\"" + DriveId + "\"}}" +
                   "]}"));

        List<DriveFolder> result = await service.GetRootFoldersAsync(AccessToken, TestContext.Current.CancellationToken);

        result.Select(folder => folder.Name).ShouldBe(["Alpha", "Mango", "Zebra"]);
    }

    [Fact]
    public async Task when_get_child_folders_is_called_then_only_folders_are_returned()
    {
        GraphService service = CreateService();
        const string parentId = "parent-folder-id";
        _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + DriveId + "/items/" + parentId + "/children").UsingGet())
               .RespondWith(JsonResponse(
                   "{\"value\":[" +
                   "{\"id\":\"cf1\",\"name\":\"ChildFolder\",\"folder\":{},\"parentReference\":{\"id\":\"" + parentId + "\",\"driveId\":\"" + DriveId + "\"}}," +
                   "{\"id\":\"cf-file\",\"name\":\"report.xlsx\",\"file\":{},\"parentReference\":{\"id\":\"" + parentId + "\",\"driveId\":\"" + DriveId + "\"}}" +
                   "]}"));

        List<DriveFolder> result = await service.GetChildFoldersAsync(AccessToken, DriveId, parentId, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("ChildFolder");
    }

    [Fact]
    public async Task when_get_child_folders_is_called_then_results_are_sorted_by_name()
    {
        GraphService service = CreateService();
        const string parentId = "sort-parent-id";
        _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + DriveId + "/items/" + parentId + "/children").UsingGet())
               .RespondWith(JsonResponse(
                   "{\"value\":[" +
                   "{\"id\":\"cfz\",\"name\":\"Zulu\",\"folder\":{},\"parentReference\":{\"id\":\"" + parentId + "\",\"driveId\":\"" + DriveId + "\"}}," +
                   "{\"id\":\"cfa\",\"name\":\"Alpha\",\"folder\":{},\"parentReference\":{\"id\":\"" + parentId + "\",\"driveId\":\"" + DriveId + "\"}}" +
                   "]}"));

        List<DriveFolder> result = await service.GetChildFoldersAsync(AccessToken, DriveId, parentId, TestContext.Current.CancellationToken);

        result.Select(folder => folder.Name).ShouldBe(["Alpha", "Zulu"]);
    }

    [Fact]
    public async Task when_get_quota_is_called_then_returns_total_and_used_bytes()
    {
        GraphService service = CreateService();
        StubMeDrive(DriveId);
        StubDriveRoot(DriveId, RootId);
        _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + DriveId).UsingGet())
               .RespondWith(JsonResponse("{\"quota\":{\"total\":1073741824,\"used\":536870912}}"));

        (var total, var used) = await service.GetQuotaAsync(AccessToken, TestContext.Current.CancellationToken);

        total.ShouldBe(1073741824L);
        used.ShouldBe(536870912L);
    }

    [Fact]
    public async Task when_get_quota_is_called_and_quota_property_is_absent_then_returns_zeroes()
    {
        GraphService service = CreateService();
        StubMeDrive(DriveId);
        StubDriveRoot(DriveId, RootId);
        _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + DriveId).UsingGet())
               .RespondWith(JsonResponse("{\"id\":\"" + DriveId + "\"}"));

        (var total, var used) = await service.GetQuotaAsync(AccessToken, TestContext.Current.CancellationToken);

        total.ShouldBe(0L);
        used.ShouldBe(0L);
    }

    [Fact]
    public async Task when_get_delta_is_called_with_null_delta_link_then_performs_full_enumeration_and_has_more_pages_is_false()
    {
        GraphService service = CreateService();
        const string folderId = "folder-full-enum";
        StubMeDrive(DriveId);
        StubDriveRoot(DriveId, RootId);
        StubFolderItem(DriveId, folderId, "MyFolder");
        _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + DriveId + "/items/" + folderId + "/children").UsingGet())
               .RespondWith(JsonResponse(
                   "{\"value\":[" +
                   "{\"id\":\"child-file\",\"name\":\"file.txt\",\"file\":{},\"parentReference\":{\"driveId\":\"" + DriveId + "\",\"id\":\"" + folderId + "\"},\"size\":1024}" +
                   "]}"));
        _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + DriveId + "/items/" + folderId + "/delta()").UsingGet())
               .RespondWith(JsonResponse(
                   "{\"value\":[],\"@odata.deltaLink\":\"" + _server.Url + "/v1.0/drives/" + DriveId + "/items/" + folderId + "/delta?token=tok1\"}"));

        DeltaResult result = await service.GetDeltaAsync(AccessToken, folderId, null, TestContext.Current.CancellationToken);

        result.HasMorePages.ShouldBeFalse();
        result.Items.Count.ShouldBe(1);
        result.Items[0].Name.ShouldBe("file.txt");
    }

    [Fact]
    public async Task when_get_delta_is_called_with_null_delta_link_then_next_delta_link_is_set_from_response()
    {
        GraphService service = CreateService();
        const string folderId = "folder-link-full";
        var expectedDeltaLink = _server.Url + "/v1.0/drives/" + DriveId + "/items/" + folderId + "/delta?token=first";
        StubMeDrive(DriveId);
        StubDriveRoot(DriveId, RootId);
        StubFolderItem(DriveId, folderId, "LinkFolder");
        _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + DriveId + "/items/" + folderId + "/children").UsingGet())
               .RespondWith(JsonResponse("{\"value\":[]}"));
        _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + DriveId + "/items/" + folderId + "/delta()").UsingGet())
               .RespondWith(JsonResponse(
                   "{\"value\":[],\"@odata.deltaLink\":\"" + expectedDeltaLink + "\"}"));

        DeltaResult result = await service.GetDeltaAsync(AccessToken, folderId, null, TestContext.Current.CancellationToken);

        result.NextDeltaLink.ShouldBe(expectedDeltaLink);
    }

    [Fact]
    public async Task when_get_delta_is_called_with_a_delta_link_then_uses_the_provided_delta_link_url()
    {
        GraphService service = CreateService();
        const string folderId = "folder-incr";
        var deltaToken = "incremental-token-abc";
        var deltaLink = _server.Url + "/v1.0/drives/" + DriveId + "/items/" + folderId + "/delta?token=" + deltaToken;
        var nextDeltaLink = _server.Url + "/v1.0/drives/" + DriveId + "/items/" + folderId + "/delta?token=new-token";
        StubMeDrive(DriveId);
        StubDriveRoot(DriveId, RootId);
        StubFolderItem(DriveId, folderId, "IncrFolder");
        _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + DriveId + "/items/" + folderId + "/delta").UsingGet())
               .RespondWith(JsonResponse(
                   "{\"value\":[{\"id\":\"upd-file\",\"name\":\"changed.txt\",\"parentReference\":{\"driveId\":\"" + DriveId + "\",\"id\":\"" + folderId + "\",\"path\":\"/drive/root:/IncrFolder\"},\"size\":512}]," +
                   "\"@odata.deltaLink\":\"" + nextDeltaLink + "\"}"));

        DeltaResult result = await service.GetDeltaAsync(AccessToken, folderId, deltaLink, TestContext.Current.CancellationToken);

        result.Items.Count.ShouldBe(1);
        result.Items[0].Name.ShouldBe("changed.txt");
        result.HasMorePages.ShouldBeFalse();
    }

    [Fact]
    public async Task when_get_delta_is_called_with_a_delta_link_then_next_delta_link_is_updated()
    {
        GraphService service = CreateService();
        const string folderId = "folder-nxtlnk";
        var deltaLink = _server.Url + "/v1.0/drives/" + DriveId + "/items/" + folderId + "/delta?token=old";
        var expectedNextLink = _server.Url + "/v1.0/drives/" + DriveId + "/items/" + folderId + "/delta?token=new";
        StubMeDrive(DriveId);
        StubDriveRoot(DriveId, RootId);
        StubFolderItem(DriveId, folderId, "NxtLnkFolder");
        _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + DriveId + "/items/" + folderId + "/delta").UsingGet())
               .RespondWith(JsonResponse(
                   "{\"value\":[],\"@odata.deltaLink\":\"" + expectedNextLink + "\"}"));

        DeltaResult result = await service.GetDeltaAsync(AccessToken, folderId, deltaLink, TestContext.Current.CancellationToken);

        result.NextDeltaLink.ShouldBe(expectedNextLink);
    }

    [Fact]
    public async Task when_get_drive_id_is_called_twice_with_same_token_then_me_drive_endpoint_is_hit_only_once()
    {
        GraphService service = CreateService();
        StubMeDrive(DriveId);
        StubDriveRoot(DriveId, RootId);

        _ = await service.GetDriveIdAsync(AccessToken, TestContext.Current.CancellationToken);
        _ = await service.GetDriveIdAsync(AccessToken, TestContext.Current.CancellationToken);

        _server.LogEntries
               .Count(entry => entry.RequestMessage.Path == "/v1.0/me/drive")
               .ShouldBe(1);
    }

    [Fact]
    public async Task when_get_drive_id_is_called_twice_with_same_token_then_both_calls_return_same_drive_id()
    {
        GraphService service = CreateService();
        StubMeDrive(DriveId);
        StubDriveRoot(DriveId, RootId);

        var firstResult = await service.GetDriveIdAsync(AccessToken, TestContext.Current.CancellationToken);
        var secondResult = await service.GetDriveIdAsync(AccessToken, TestContext.Current.CancellationToken);

        firstResult.ShouldBe(DriveId);
        secondResult.ShouldBe(DriveId);
    }

    private GraphService CreateService()
    {
        HttpClient httpClient = _server.CreateClient();
        httpClient.BaseAddress = new Uri(_server.Url! + "/v1.0");
        return new GraphService(httpClient);
    }

    private void StubMeDrive(string driveId)
        => _server.Given(WireMockRequest.Create().WithPath("/v1.0/me/drive").UsingGet())
               .RespondWith(JsonResponse("{\"id\":\"" + driveId + "\"}"));

    private void StubDriveRoot(string driveId, string rootId)
        => _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + driveId + "/root").UsingGet())
               .RespondWith(JsonResponse("{\"id\":\"" + rootId + "\"}"));

    private void StubFolderItem(string driveId, string folderId, string name)
        => _server.Given(WireMockRequest.Create().WithPath("/v1.0/drives/" + driveId + "/items/" + folderId).UsingGet())
               .RespondWith(JsonResponse("{\"id\":\"" + folderId + "\",\"name\":\"" + name + "\"}"));

    private static IResponseBuilder JsonResponse(string json)
        => Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(json);
}
