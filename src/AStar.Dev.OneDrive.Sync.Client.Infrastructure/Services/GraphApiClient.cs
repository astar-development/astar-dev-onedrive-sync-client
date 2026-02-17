using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
///     Wrapper implementation for Microsoft Graph API client.
/// </summary>
/// <remarks>
///     Wraps GraphServiceClient to provide a testable abstraction for OneDrive operations.
/// </remarks>
public sealed class GraphApiClient(IAuthService authService, HttpClient http, MsalConfigurationSettings msalConfigurationSettings) : IGraphApiClient
{
    public async Task<DeltaPage> GetDriveDeltaPageAsync(string accountId, HashedAccountId hashedAccountId, string? deltaOrNextLink, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var url = GetDeltaOrNextUrl(deltaOrNextLink);

        var token = await authService.GetAccessTokenAsync(accountId, cancellationToken);
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using HttpResponseMessage response = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        _ = response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using JsonDocument doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        List<DriveItemEntity> items = ParseDriveItemRecords(hashedAccountId, doc);

        var next = TryGetODataProperty(doc, "@odata.nextLink");
        var delta = TryGetODataProperty(doc, "@odata.deltaLink");

        return new DeltaPage(items, next, delta);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DriveItem>> GetDriveItemChildrenAsync(string accountId, HashedAccountId hashedAccountId, string itemId, CancellationToken cancellationToken = default) => await GetDriveItemChildrenAsync(accountId, hashedAccountId, itemId, 200, cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<DriveItem>> GetDriveItemChildrenAsync(string accountId, HashedAccountId hashedAccountId, string itemId, int maxItemsInBatch = 200, CancellationToken cancellationToken = default)
    {
        await DebugLog.EntryAsync("GraphApiClient.GetDriveItemChildrenAsync", hashedAccountId, cancellationToken);
        _ = await DebugLog.LogInfoAsync("GraphApiClient.GetDriveItemChildrenAsync", hashedAccountId, $"Fetching children for item ID: {itemId}", cancellationToken);

        GraphServiceClient graphClient = CreateGraphClientAsync(accountId, hashedAccountId);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if(drive?.Id is null)
        {
            _ = await DebugLog.LogErrorAsync("GraphApiClient.GetDriveItemChildrenAsync", hashedAccountId, "Drive ID is null", null, cancellationToken);
            await DebugLog.ExitAsync("GraphApiClient.GetDriveItemChildrenAsync", hashedAccountId, cancellationToken);
            return [];
        }

        _ = await DebugLog.LogInfoAsync("GraphApiClient.GetDriveItemChildrenAsync", hashedAccountId, $"Using drive ID: {drive.Id}", cancellationToken);

        var itemsList = new List<DriveItem>();
        string? nextLink = null;
        do
        {
            DriveItemCollectionResponse? response = nextLink is null
                ? await graphClient.Drives[drive.Id].Items[itemId].Children.GetAsync(q => q.QueryParameters.Top = maxItemsInBatch, cancellationToken)
                : await graphClient.RequestAdapter.SendAsync(new RequestInformation { HttpMethod = Method.GET, URI = new Uri(nextLink) },DriveItemCollectionResponse.CreateFromDiscriminatorValue,null,cancellationToken);

            _ = await DebugLog.LogInfoAsync("GraphApiClient.GetDriveItemChildrenAsync", hashedAccountId, $"Response received - Value count: {response?.Value?.Count ?? 0}, NextLink: {response?.OdataNextLink}", cancellationToken);

            IEnumerable<DriveItem> items = response?.Value?.Where(item => item.Deleted is null) ?? [];
            itemsList.AddRange(items);

            nextLink = response?.OdataNextLink;
        } while(!string.IsNullOrEmpty(nextLink));

        _ = await DebugLog.LogInfoAsync("GraphApiClient.GetDriveItemChildrenAsync", hashedAccountId, $"After filtering deleted items: {itemsList.Count} items", cancellationToken);
        await DebugLog.ExitAsync("GraphApiClient.GetDriveItemChildrenAsync", hashedAccountId, cancellationToken);
        return itemsList;
    }

    /// <inheritdoc />
    public async Task<Drive?> GetMyDriveAsync(string accountId, HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        GraphServiceClient graphClient = CreateGraphClientAsync(accountId, hashedAccountId  );
        return await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DriveItem?> GetDriveRootAsync(string accountId, HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        GraphServiceClient graphClient = CreateGraphClientAsync(accountId, hashedAccountId);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        return drive?.Id is null ? null : await graphClient.Drives[drive.Id].Root.GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DriveItem?> GetDriveItemAsync(string accountId, HashedAccountId hashedAccountId, string itemId, CancellationToken cancellationToken = default)
    {
        GraphServiceClient graphClient = CreateGraphClientAsync(accountId, hashedAccountId);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        return drive?.Id is null ? null : await graphClient.Drives[drive.Id].Items[itemId].GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DriveItem>> GetRootChildrenAsync(string accountId, HashedAccountId hashedAccountId, CancellationToken cancellationToken = default)
    {
        GraphServiceClient graphClient = CreateGraphClientAsync(accountId, hashedAccountId);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if(drive?.Id is null)
            return [];

        DriveItem? root = await graphClient.Drives[drive.Id].Root.GetAsync(cancellationToken: cancellationToken);
        if(root?.Id is null)
            return [];

        DriveItemCollectionResponse? response = await graphClient.Drives[drive.Id].Items[root.Id].Children.GetAsync(cancellationToken: cancellationToken);
        return response?.Value ?? Enumerable.Empty<DriveItem>();
    }

    /// <inheritdoc />
    public async Task DownloadFileAsync(string accountId,  HashedAccountId hashedAccountId,string itemId, string localFilePath, CancellationToken cancellationToken = default)
    {
        GraphServiceClient graphClient = CreateGraphClientAsync(accountId, hashedAccountId);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if(drive?.Id is null)
            throw new InvalidOperationException("Unable to access user's drive");

        Stream contentStream = await graphClient.Drives[drive.Id].Items[itemId].Content.GetAsync(cancellationToken: cancellationToken) ??
                               throw new InvalidOperationException($"Failed to download file content for item {itemId}");

        var directory = Path.GetDirectoryName(localFilePath);
        if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            _ = Directory.CreateDirectory(directory);

        using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await contentStream.CopyToAsync(fileStream, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DriveItem> UploadFileAsync(string accountId, HashedAccountId hashedAccountId, string localFilePath, string remotePath, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        if(!File.Exists(localFilePath))
            throw new FileNotFoundException($"Local file not found: {localFilePath}", localFilePath);

        GraphServiceClient graphClient = CreateGraphClientAsync(accountId, hashedAccountId);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if(drive?.Id is null)
            throw new InvalidOperationException("Unable to access user's drive");

        var fileInfo = new FileInfo(localFilePath);
        const long smallFileThresholdMb = 4 * 1024 * 1024; // 4MB - Graph API threshold for simple upload

        var normalizedPath = remotePath.TrimStart('/');

        if(fileInfo.Length < smallFileThresholdMb)
        {
            using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            DriveItem uploadedItem = await graphClient.Drives[drive.Id]
                .Items[$"root:/{normalizedPath}:"]
                .Content
                .PutAsync(fileStream, cancellationToken: cancellationToken) ?? throw new InvalidOperationException($"Upload failed for file: {localFilePath}");

            progress?.Report(fileInfo.Length);

            return uploadedItem;
        }
        else
        {
            var requestBody = new CreateUploadSessionPostRequestBody
            {
                Item = new DriveItemUploadableProperties { AdditionalData = new Dictionary<string, object> { { "@microsoft.graph.conflictBehavior", "replace" } } }
            };

            UploadSession? uploadSession = await graphClient.Drives[drive.Id]
                .Items[$"root:/{normalizedPath}:"]
                .CreateUploadSession
                .PostAsync(requestBody, cancellationToken: cancellationToken);

            if(uploadSession?.UploadUrl is null)
                throw new InvalidOperationException($"Failed to create upload session for file: {localFilePath}");

            const int ChunkSize = 5 * 1024 * 1024; // 5MB chunks
            using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            long position = 0;
            var totalLength = fileStream.Length;

            using var httpClient = new HttpClient();

            while(position < totalLength)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var chunkSize = (int)Math.Min(ChunkSize, totalLength - position);
                var chunk = new byte[chunkSize];
                var bytesRead = await fileStream.ReadAsync(chunk.AsMemory(0, chunkSize), cancellationToken);

                if(bytesRead != chunkSize)
                    throw new InvalidOperationException($"Failed to read expected bytes from file. Expected: {chunkSize}, Read: {bytesRead}");

                var contentRange = $"bytes {position}-{position + chunkSize - 1}/{totalLength}";

                using var content = new ByteArrayContent(chunk);
                content.Headers.ContentLength = chunkSize;
                content.Headers.ContentRange = new ContentRangeHeaderValue(position, position + chunkSize - 1, totalLength);

                HttpResponseMessage response = await httpClient.PutAsync(uploadSession.UploadUrl, content, cancellationToken);

                if(!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Accepted)
                    throw new InvalidOperationException($"Chunk upload failed. Status: {response.StatusCode}, Range: {contentRange}");

                position += chunkSize;

                progress?.Report(position);

                if(position >= totalLength && response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    DriveItem uploadedItem = JsonSerializer.Deserialize<DriveItem>(responseContent) ?? throw new InvalidOperationException("Failed to deserialize uploaded DriveItem from response");
                    return uploadedItem;
                }
            }

            throw new InvalidOperationException("Upload completed but no DriveItem was returned");
        }
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(string accountId, HashedAccountId hashedAccountId, string itemId, CancellationToken cancellationToken = default)
    {
        await DebugLog.EntryAsync("GraphApiClient.DeleteFileAsync", hashedAccountId, cancellationToken);
        _ = await DebugLog.LogInfoAsync("GraphApiClient.DeleteFileAsync", hashedAccountId, $"Attempting to delete remote file. HashedAccountId: {hashedAccountId}, ItemId: {itemId}", cancellationToken);

        GraphServiceClient graphClient = CreateGraphClientAsync(accountId, hashedAccountId);
        Drive? drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if(drive?.Id is null)
        {
            _ = await DebugLog.LogErrorAsync("GraphApiClient.DeleteFileAsync", hashedAccountId, "Drive ID is null. Cannot delete file.", null, cancellationToken);
            await DebugLog.ExitAsync("GraphApiClient.DeleteFileAsync", hashedAccountId, cancellationToken);
            throw new InvalidOperationException("Unable to access user's drive");
        }

        try
        {
            await graphClient.Drives[drive.Id].Items[itemId].DeleteAsync(cancellationToken: cancellationToken);
            _ = await DebugLog.LogInfoAsync("GraphApiClient.DeleteFileAsync", hashedAccountId, $"Successfully deleted remote file. HashedAccountId: {hashedAccountId}, ItemId: {itemId}", cancellationToken);
        }
        catch(Exception ex)
        {
            _ = await DebugLog.LogErrorAsync("GraphApiClient.DeleteFileAsync", hashedAccountId, $"Exception during remote file deletion. HashedAccountId: {hashedAccountId}, ItemId: {itemId}", ex, cancellationToken);
            throw;
        }
        finally
        {
            await DebugLog.ExitAsync("GraphApiClient.DeleteFileAsync", hashedAccountId, cancellationToken);
        }
    }

    private GraphServiceClient CreateGraphClientAsync(string accountId,HashedAccountId hashedAccountId)
    {
        var authProvider = new BaseBearerTokenAuthenticationProvider(new GraphTokenProvider(authService, accountId, hashedAccountId));

        return new GraphServiceClient(authProvider);
    }

    private string GetDeltaOrNextUrl(string? deltaOrNextLink) => string.IsNullOrEmpty(deltaOrNextLink)
                ? $"{msalConfigurationSettings.GraphUri}/root/delta"
                : deltaOrNextLink;

    private sealed class GraphTokenProvider(IAuthService authService, string accountId, HashedAccountId hashedAccountId) : IAccessTokenProvider
    {
        public AllowedHostsValidator AllowedHostsValidator => new();

        public async Task<string> GetAuthorizationTokenAsync(Uri uri, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
        {
            var token = await authService.GetAccessTokenAsync(accountId, cancellationToken) ?? throw new InvalidOperationException($"Failed to acquire access token for account: {hashedAccountId}");
            return token;
        }
    }

    private static List<DriveItemEntity> ParseDriveItemRecords(HashedAccountId hashedAccountId, JsonDocument doc)
    {
        var items = new List<DriveItemEntity>();
        if(doc.RootElement.TryGetProperty("value", out JsonElement arr))
        {
            foreach(JsonElement el in arr.EnumerateArray())
            {
                items.Add(ParseDriveItemRecord(hashedAccountId, el));
            }
        }

        return items;
    }

    private static DriveItemEntity ParseDriveItemRecord(HashedAccountId hashedAccountId, JsonElement jsonElement)
    {
        var driveItemId = jsonElement.GetProperty("id").GetString()!;
        var isFolder = jsonElement.TryGetProperty("folder", out _);
        var size = jsonElement.TryGetProperty("size", out JsonElement sProp) ? sProp.GetInt64() : 0L;
        var parentPath = SetParentPath(jsonElement);
        var name = jsonElement.TryGetProperty("name", out JsonElement n) ? n.GetString() ?? driveItemId : driveItemId;
        var relativePath = GraphPathHelpers.BuildRelativePath(parentPath, name);
        var eTag = jsonElement.TryGetProperty("eTag", out JsonElement et) ? et.GetString() : null;
        var cTag = jsonElement.TryGetProperty("cTag", out JsonElement ctProp) ? ctProp.GetString() : null;
        DateTimeOffset lastModifiedUtc = GetLastModifiedUtc(jsonElement);
        var isDeleted = jsonElement.TryGetProperty("deleted", out _);

        return new DriveItemEntity(hashedAccountId, driveItemId, relativePath, eTag, cTag, size, lastModifiedUtc, isFolder, isDeleted);
    }

    private static DateTimeOffset GetLastModifiedUtc(JsonElement jsonElement) => jsonElement.TryGetProperty("lastModifiedDateTime", out JsonElement lm)
                ? DateTimeOffset.Parse(lm.GetString()!, CultureInfo.InvariantCulture)
                : DateTimeOffset.UtcNow;

    private static string SetParentPath(JsonElement jsonElement) => jsonElement.TryGetProperty("parentReference", out JsonElement pr) && pr.TryGetProperty("path", out JsonElement p) ? p.GetString() ?? string.Empty : string.Empty;

    private static string? TryGetODataProperty(JsonDocument doc, string propertyName) => doc.RootElement.TryGetProperty(propertyName, out JsonElement prop) ? prop.GetString() : null;
}
