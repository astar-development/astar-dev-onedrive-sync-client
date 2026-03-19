using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.Utilities;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.Delta;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using Serilog;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Graph;

public sealed class GraphService : IGraphService
{
    private readonly UploadService _uploadService = new();
    private readonly Dictionary<string, DriveContext> _cache = [];

    /// <inheritdoc />
    public async Task<string> GetDriveIdAsync(string accessToken, CancellationToken ct = default)
        => (await ResolveClientWithDriveContextAsync(accessToken, ct)).Ctx.DriveId;

    /// <inheritdoc />
    public async Task<List<DriveFolder>> GetRootFoldersAsync(string accessToken, CancellationToken ct = default)
    {
        (GraphServiceClient? client, DriveContext? driveContext) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        DriveItemCollectionResponse? driveItemCollectionResponse = await client.Drives[driveContext.DriveId].Items[driveContext.RootId].Children
            .GetAsync(req => req.QueryParameters.Select =    ["id", "name", "folder", "file", "size",     "lastModifiedDateTime", "parentReference",     "@microsoft.graph.downloadUrl"], ct);

        List<DriveFolder> folders = [];

        DriveItemCollectionResponse? page = driveItemCollectionResponse;
        while(page?.Value is not null)
        {
            folders.AddRange(
                page.Value
                    .Where(i => i.Folder is not null)
                    .Select(i => new DriveFolder(
                        Id: i.Id!,
                        Name: i.Name!,
                        ParentId: i.ParentReference?.Id)));

            if(page.OdataNextLink is null)
                break;

            page = await client.Drives[driveContext.DriveId].Items[driveContext.RootId].Children
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: ct);
        }

        return [.. folders.OrderBy(f => f.Name)];
    }

    /// <inheritdoc />
    public async Task<List<DriveFolder>> GetChildFoldersAsync(string accessToken, string driveId, string parentFolderId, CancellationToken ct = default)
    {
        GraphServiceClient client = BuildClient(accessToken);

        DriveItemCollectionResponse? result = await client.Drives[driveId].Items[parentFolderId].Children
            .GetAsync(req =>
            {
                req.QueryParameters.Select = ["id", "name", "folder", "parentReference"];
                req.QueryParameters.Top = 100;
            }, ct);

        List<DriveFolder> folders = [];

        DriveItemCollectionResponse? page = result;
        while(page?.Value is not null)
        {
            folders.AddRange(
                page.Value
                    .Where(i => i.Folder is not null)
                    .Select(i => new DriveFolder(
                        Id: i.Id!,
                        Name: i.Name!,
                        ParentId: i.ParentReference?.Id)));

            if(page.OdataNextLink is null)
                break;

            page = await client.Drives[driveId].Items[parentFolderId].Children
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: ct);
        }

        return [.. folders.OrderBy(f => f.Name)];
    }

    /// <inheritdoc />
    public async Task<(long Total, long Used)> GetQuotaAsync(string accessToken, CancellationToken ct = default)
    {
        (GraphServiceClient? client, DriveContext? ctx) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        Drive? drive = await client.Drives[ctx.DriveId]
            .GetAsync(req => req.QueryParameters.Select = ["quota"], ct);

        return drive?.Quota is { Total: not null, Used: not null }
            ? (drive.Quota.Total!.Value, drive.Quota.Used!.Value)
            : (0L, 0L);
    }

    /// <inheritdoc />
    public async Task<DeltaResult> GetDeltaAsync(string accessToken, string folderId, string? deltaLink, CancellationToken ct = default)
    {
        (GraphServiceClient? client, DriveContext? ctx) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        DriveItem? folderItem = await client.Drives[ctx.DriveId]
        .Items[folderId]
        .GetAsync(req =>
            req.QueryParameters.Select = ["id", "name"], ct);

        var folderName = folderItem?.Name ?? string.Empty;

        if(deltaLink is null)
        {
            return await FullEnumerationAsync(
            client, ctx.DriveId, folderId, folderName, ct);
        }

        List<DeltaItem> items = [];
        string? nextDeltaLink = null;
        var hasMorePages      = false;

        DeltaGetResponse? page = await client.Drives[ctx.DriveId].Items[folderId].Delta
                                            .WithUrl(deltaLink)
                                            .GetAsDeltaGetResponseAsync(cancellationToken: ct);

        while(page?.Value is not null)
        {
            foreach(DriveItem item in page.Value)
                items.Add(MapToDeltaItem(item));

            if(page.OdataNextLink is not null)
            {
                hasMorePages = true;
                page = await client.Drives[ctx.DriveId].Items[folderId].Delta
                    .WithUrl(page.OdataNextLink)
                    .GetAsDeltaGetResponseAsync(cancellationToken: ct);
            }
            else
            {
                nextDeltaLink = page.OdataDeltaLink;
                break;
            }
        }

        return new DeltaResult(items, nextDeltaLink, hasMorePages);
    }

    /// <inheritdoc />
    public async Task<string> UploadFileAsync(string accessToken, string localPath, string remotePath, string parentFolderId, CancellationToken ct = default)
    {
        (GraphServiceClient? client, DriveContext? ctx) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        return await _uploadService.UploadAsync(client, ctx.DriveId, parentFolderId, localPath, remotePath, ct: ct);
    }

    private async Task<DeltaResult> FullEnumerationAsync(GraphServiceClient client, string driveId, string folderId, string folderName, CancellationToken ct)
    {
        List<DeltaItem> items = [];
        await EnumerateSubFolderAsync(client, driveId, folderId, folderName, items, ct);
        DeltaGetResponse? deltaPage = await GetDeltaLinkForNextSync(client, driveId, folderId, ct);

        var deltaLink = deltaPage?.OdataDeltaLink;

        return new DeltaResult(items, deltaLink, false);
    }

    private static async Task<DeltaGetResponse?> GetDeltaLinkForNextSync(GraphServiceClient client, string driveId, string folderId, CancellationToken ct)
            => await client.Drives[driveId].Items[folderId].Delta.GetAsDeltaGetResponseAsync(cancellationToken: ct);

    private async Task EnumerateSubFolderAsync(GraphServiceClient client, string driveId, string parentId, string relativePath, List<DeltaItem> items, CancellationToken ct)
    {
        DriveItemCollectionResponse? page = await client.Drives[driveId].Items[parentId].Children.GetAsync();

        while(page?.Value is not null)
        {
            foreach(DriveItem item in page.Value)
            {
                var itemPath = string.IsNullOrEmpty(relativePath)
                ? item.Name ?? string.Empty
                : $"{relativePath}/{item.Name}";

                items.Add(new DeltaItem(
                    Id: item.Id!,
                    DriveId: item.ParentReference?.DriveId ?? string.Empty,
                    Name: item.Name ?? string.Empty,
                    ParentId: item.ParentReference?.Id,
                    IsFolder: item.Folder is not null,
                    IsDeleted: false,
                    Size: item.Size ?? 0L,
                    LastModified: item.LastModifiedDateTime,
                    DownloadUrl: item.AdditionalData
                        .TryGetValue("@microsoft.graph.downloadUrl", out var url)
                            ? url?.ToString()
                            : null,
                    RelativePath: itemPath));

                if(item.Folder is not null && item.Id is not null)
                {
                    await EnumerateSubFolderAsync(client, driveId, item.Id, itemPath, items, ct);
                }
            }

            if(page.OdataNextLink is null)
                break;

            page = await client.Drives[driveId].Items[parentId].Children
                .WithUrl(page.OdataNextLink)
                .GetAsync(cancellationToken: ct);
        }
    }

    private static DeltaItem MapToDeltaItem(DriveItem item)
    {
        var parentPath = item.ParentReference?.Path ?? string.Empty;
        var rootMarker = "root:";
        var afterRoot  = parentPath.Contains(rootMarker)
                            ? parentPath[(parentPath.IndexOf(rootMarker) + rootMarker.Length)..].TrimStart('/')
                            : string.Empty;

        var relativePath = string.IsNullOrEmpty(afterRoot)
                            ? item.Name ?? string.Empty
                            : $"{afterRoot}/{item.Name}";

        return new DeltaItem(
            Id: item.Id!,
            DriveId: item.ParentReference?.DriveId ?? string.Empty,
            Name: item.Name ?? string.Empty,
            ParentId: item.ParentReference?.Id,
            IsFolder: item.Folder is not null,
            IsDeleted: item.Deleted is not null,
            Size: item.Size ?? 0L,
            LastModified: item.LastModifiedDateTime,
            DownloadUrl: item.AdditionalData
                .TryGetValue("@microsoft.graph.downloadUrl", out var url)
                    ? url?.ToString()
                    : null,
            RelativePath: relativePath);
    }

    private async Task<(GraphServiceClient Client, DriveContext Ctx)> ResolveClientWithDriveContextAsync(string accessToken, CancellationToken ct)
    {
        GraphServiceClient graphServiceClient = BuildClient(accessToken);

        if(_cache.TryGetValue(accessToken, out DriveContext? cached))
            return (graphServiceClient, cached);

        Drive? drive = await graphServiceClient.Me.Drive.GetAsync(cancellationToken: ct);

        var driveId = drive?.Id ?? throw new InvalidOperationException("Could not retrieve drive ID.");

        DriveItem? root = await graphServiceClient.Drives[driveId].Root.GetAsync(cancellationToken: ct);

        var rootId = root?.Id ?? throw new InvalidOperationException("Could not retrieve root item ID.");

        var driveContext = new DriveContext(driveId, rootId);
        _cache[accessToken] = driveContext;

        return (graphServiceClient, driveContext);
    }

    private static GraphServiceClient BuildClient(string accessToken)
        => new(new BaseBearerTokenAuthenticationProvider(new StaticAccessTokenProvider(accessToken)));

    private sealed record DriveContext(string DriveId, string RootId);

    private sealed class StaticAccessTokenProvider(string token) : IAccessTokenProvider
    {
        public Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken ct = default) => Task.FromResult(token);

        public AllowedHostsValidator AllowedHostsValidator { get; } = new(["graph.microsoft.com"]);
    }
}
