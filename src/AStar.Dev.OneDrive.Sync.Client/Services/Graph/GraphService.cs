using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.Utilities;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.Delta;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using Serilog;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Graph;

public sealed class GraphService : IGraphService
{
    private readonly Dictionary<string, DriveContext> _cache = [];

    // ── IGraphService ─────────────────────────────────────────────────────

    /// <inheritdoc />
    public async Task<string> GetDriveIdAsync(string accessToken, CancellationToken ct = default)
        => (await ResolveClientWithDriveContextAsync(accessToken, ct)).Ctx.DriveId;

    /// <inheritdoc />
    public async Task<List<DriveFolder>> GetRootFoldersAsync(string accessToken, CancellationToken ct = default)
    {
        (GraphServiceClient? client, DriveContext? driveContext) = await ResolveClientWithDriveContextAsync(accessToken, ct);

        DriveItemCollectionResponse? driveItemCollectionResponse = await client.Drives[driveContext.DriveId].Items[driveContext.RootId].Children
            .GetAsync(req =>
            {
                req.QueryParameters.Select = ["id", "name", "folder", "parentReference"];
                req.QueryParameters.Top = 100;
            }, ct);

        List<DriveFolder> folders = [];

        DriveItemCollectionResponse? page = driveItemCollectionResponse;
        while (page?.Value is not null)
        {
            folders.AddRange(
                page.Value
                    .Where(i => i.Folder is not null)
                    .Select(i => new DriveFolder(
                        Id: i.Id!,
                        Name: i.Name!,
                        ParentId: i.ParentReference?.Id)));

            if (page.OdataNextLink is null) break;

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
        while (page?.Value is not null)
        {
            folders.AddRange(
                page.Value
                    .Where(i => i.Folder is not null)
                    .Select(i => new DriveFolder(
                        Id: i.Id!,
                        Name: i.Name!,
                        ParentId: i.ParentReference?.Id)));

            if (page.OdataNextLink is null) break;

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
    public async Task<DeltaResult> GetDeltaAsync(
    string  accessToken,
    string  folderId,
    string? deltaLink,
    CancellationToken ct = default)
{
    (GraphServiceClient? client, DriveContext? ctx) = await ResolveClientWithDriveContextAsync(accessToken, ct);

    // First sync — enumerate all children recursively then get a delta link
    if (deltaLink is null)
        return await FullEnumerationAsync(client, ctx.DriveId, folderId, ct);

    // Subsequent syncs — use delta link for changes only
    List<DeltaItem> items = [];
    string? nextDeltaLink = null;
    bool hasMorePages     = false;

        DeltaGetResponse? page = await client.Drives[ctx.DriveId].Items[folderId].Delta
        .WithUrl(deltaLink)
        .GetAsDeltaGetResponseAsync(cancellationToken: ct);

    while (page?.Value is not null)
    {
        foreach (DriveItem item in page.Value)
            items.Add(MapToDeltaItem(item));

        if (page.OdataNextLink is not null)
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

private async Task<DeltaResult> FullEnumerationAsync(
    GraphServiceClient client,
    string             driveId,
    string             folderId,
    CancellationToken  ct)
{
    List<DeltaItem> items = [];
    await EnumerateFolderAsync(client, driveId, folderId, string.Empty, items, ct);

        // After full enumeration, get a delta link to use for future syncs
        DeltaGetResponse? deltaPage = await client.Drives[driveId].Items[folderId].Delta
        .GetAsDeltaGetResponseAsync(cancellationToken: ct);

    var deltaLink = deltaPage?.OdataDeltaLink;

    return new DeltaResult(items, deltaLink, false);
}

private async Task EnumerateFolderAsync(
    GraphServiceClient client,
    string             driveId,
    string             parentId,
    string             relativePath,
    List<DeltaItem>    items,
    CancellationToken  ct)
{
        DriveItemCollectionResponse? page = await client.Drives[driveId].Items[parentId].Children
        .GetAsync(req =>
        {
            req.QueryParameters.Select = 
                ["id", "name", "folder", "file", "size", 
                 "lastModifiedDateTime", "parentReference",
                 "@microsoft.graph.downloadUrl"];
            req.QueryParameters.Top = 100;
        }, ct);

    while (page?.Value is not null)
    {
        foreach (DriveItem item in page.Value)
        {
            var itemPath = string.IsNullOrEmpty(relativePath)
                ? item.Name ?? string.Empty
                : $"{relativePath}/{item.Name}";

            items.Add(new DeltaItem(
                Id:           item.Id!,
                Name:         item.Name ?? string.Empty,
                ParentId:     item.ParentReference?.Id,
                IsFolder:     item.Folder is not null,
                IsDeleted:    false,
                Size:         item.Size ?? 0L,
                LastModified: item.LastModifiedDateTime,
                DownloadUrl:  item.AdditionalData
                    .TryGetValue("@microsoft.graph.downloadUrl", out var url)
                        ? url?.ToString()
                        : null));

            // Recurse into subfolders
            if (item.Folder is not null && item.Id is not null)
                await EnumerateFolderAsync(
                    client, driveId, item.Id, itemPath, items, ct);
        }

        if (page.OdataNextLink is null) break;

        page = await client.Drives[driveId].Items[parentId].Children
            .WithUrl(page.OdataNextLink)
            .GetAsync(cancellationToken: ct);
    }
}

private static DeltaItem MapToDeltaItem(Microsoft.Graph.Models.DriveItem item)
    => new(
        Id:           item.Id!,
        Name:         item.Name ?? string.Empty,
        ParentId:     item.ParentReference?.Id,
        IsFolder:     item.Folder is not null,
        IsDeleted:    item.Deleted is not null,
        Size:         item.Size ?? 0L,
        LastModified: item.LastModifiedDateTime,
        DownloadUrl:  item.AdditionalData
            .TryGetValue("@microsoft.graph.downloadUrl", out var url)
                ? url?.ToString()
                : null);

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task<(GraphServiceClient Client, DriveContext Ctx)> ResolveClientWithDriveContextAsync(string accessToken, CancellationToken ct)
    {
        GraphServiceClient graphServiceClient = BuildClient(accessToken);

        if (_cache.TryGetValue(accessToken, out DriveContext? cached))
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
        public Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken ct = default) => Task.FromResult(token);

        public AllowedHostsValidator AllowedHostsValidator { get; } = new(["graph.microsoft.com"]);
    }
}
