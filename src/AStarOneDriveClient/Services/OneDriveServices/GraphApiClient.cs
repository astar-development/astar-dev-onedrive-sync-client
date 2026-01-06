using AStarOneDriveClient.Authentication;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;

namespace AStarOneDriveClient.Services.OneDriveServices;

/// <summary>
/// Wrapper implementation for Microsoft Graph API client.
/// </summary>
/// <remarks>
/// Wraps GraphServiceClient to provide a testable abstraction for OneDrive operations.
/// </remarks>
public sealed class GraphApiClient : IGraphApiClient
{
    private readonly IAuthService _authService;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphApiClient"/> class.
    /// </summary>
    /// <param name="authService">The authentication service.</param>
    public GraphApiClient(IAuthService authService)
    {
        ArgumentNullException.ThrowIfNull(authService);
        _authService = authService;
    }

    private async Task<GraphServiceClient> CreateGraphClientAsync(string accountId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(accountId))
        {
            throw new ArgumentException("Account ID cannot be null or empty", nameof(accountId));
        }

        // Create a token provider that uses the auth service
        var authProvider = new BaseBearerTokenAuthenticationProvider(
            new GraphTokenProvider(_authService, accountId));

        return new GraphServiceClient(authProvider);
    }

    // Token provider implementation for Graph API
    private sealed class GraphTokenProvider : IAccessTokenProvider
    {
        private readonly IAuthService _authService;
        private readonly string _accountId;

        public GraphTokenProvider(IAuthService authService, string accountId)
        {
            _authService = authService;
            _accountId = accountId;
        }

        public AllowedHostsValidator AllowedHostsValidator => new();

        public async Task<string> GetAuthorizationTokenAsync(
            Uri uri,
            Dictionary<string, object>? additionalAuthenticationContext = null,
            CancellationToken cancellationToken = default)
        {
            var token = await _authService.GetAccessTokenAsync(_accountId, cancellationToken);
            if (token is null)
            {
                throw new InvalidOperationException($"Failed to acquire access token for account: {_accountId}");
            }
            return token;
        }
    }

    /// <inheritdoc/>
    public async Task<Drive?> GetMyDriveAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        return await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DriveItem?> GetDriveRootAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        var drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null)
        {
            return null;
        }

        return await graphClient.Drives[drive.Id].Root.GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DriveItem>> GetDriveItemChildrenAsync(string accountId, string itemId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemId);

        var graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        var drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null) return Enumerable.Empty<DriveItem>();

        var response = await graphClient.Drives[drive.Id].Items[itemId].Children.GetAsync(cancellationToken: cancellationToken);

        // Filter out deleted items (items in recycle bin)
        return response?.Value?.Where(item => item.Deleted is null) ?? Enumerable.Empty<DriveItem>();
    }

    /// <inheritdoc/>
    public async Task<DriveItem?> GetDriveItemAsync(string accountId, string itemId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemId);

        var graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        var drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null) return null;

        return await graphClient.Drives[drive.Id].Items[itemId].GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DriveItem>> GetRootChildrenAsync(string accountId, CancellationToken cancellationToken = default)
    {
        var graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        var drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null) return Enumerable.Empty<DriveItem>();

        var root = await graphClient.Drives[drive.Id].Root.GetAsync(cancellationToken: cancellationToken);
        if (root?.Id is null) return Enumerable.Empty<DriveItem>();

        var response = await graphClient.Drives[drive.Id].Items[root.Id].Children.GetAsync(cancellationToken: cancellationToken);
        return response?.Value ?? Enumerable.Empty<DriveItem>();
    }

    /// <inheritdoc/>
    public async Task DownloadFileAsync(string accountId, string itemId, string localFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemId);
        ArgumentNullException.ThrowIfNull(localFilePath);

        var graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        var drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null)
        {
            throw new InvalidOperationException("Unable to access user's drive");
        }

        // Download file content stream from OneDrive
        var contentStream = await graphClient.Drives[drive.Id].Items[itemId].Content.GetAsync(cancellationToken: cancellationToken);
        if (contentStream is null)
        {
            throw new InvalidOperationException($"Failed to download file content for item {itemId}");
        }

        // Ensure the directory exists
        var directory = System.IO.Path.GetDirectoryName(localFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write the stream to local file
        using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await contentStream.CopyToAsync(fileStream, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DriveItem> UploadFileAsync(string accountId, string localFilePath, string remotePath, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(localFilePath);
        ArgumentNullException.ThrowIfNull(remotePath);

        if (!File.Exists(localFilePath))
        {
            throw new FileNotFoundException($"Local file not found: {localFilePath}", localFilePath);
        }

        var graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        var drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null)
        {
            throw new InvalidOperationException("Unable to access user's drive");
        }

        var fileInfo = new FileInfo(localFilePath);
        const long SmallFileThreshold = 4 * 1024 * 1024; // 4MB - Graph API threshold for simple upload

        // Normalize remote path (remove leading slash, Graph API uses root:/{path} format)
        var normalizedPath = remotePath.TrimStart('/');

        if (fileInfo.Length < SmallFileThreshold)
        {
            // Simple upload for small files - use path-based addressing
            using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            // PUT to /drives/{driveId}/root:/{path}:/content
            var uploadedItem = await graphClient.Drives[drive.Id]
                .Items[$"root:/{normalizedPath}:"]
                .Content
                .PutAsync(fileStream, cancellationToken: cancellationToken);

            if (uploadedItem is null)
            {
                throw new InvalidOperationException($"Upload failed for file: {localFilePath}");
            }

            return uploadedItem;
        }
        else
        {
            // Resumable upload session for large files
            var requestBody = new Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession.CreateUploadSessionPostRequestBody
            {
                Item = new DriveItemUploadableProperties
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "@microsoft.graph.conflictBehavior", "replace" }
                    }
                }
            };

            var uploadSession = await graphClient.Drives[drive.Id]
                .Items[$"root:/{normalizedPath}:"]
                .CreateUploadSession
                .PostAsync(requestBody, cancellationToken: cancellationToken);

            if (uploadSession?.UploadUrl is null)
            {
                throw new InvalidOperationException($"Failed to create upload session for file: {localFilePath}");
            }

            // Upload in chunks (recommended 5-10MB per chunk for optimal performance)
            const int ChunkSize = 5 * 1024 * 1024; // 5MB chunks
            using var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

            long position = 0;
            var totalLength = fileStream.Length;

            using var httpClient = new HttpClient();

            while (position < totalLength)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var chunkSize = (int)Math.Min(ChunkSize, totalLength - position);
                var chunk = new byte[chunkSize];
                var bytesRead = await fileStream.ReadAsync(chunk.AsMemory(0, chunkSize), cancellationToken);

                if (bytesRead != chunkSize)
                {
                    throw new InvalidOperationException($"Failed to read expected bytes from file. Expected: {chunkSize}, Read: {bytesRead}");
                }

                var contentRange = $"bytes {position}-{position + chunkSize - 1}/{totalLength}";

                using var content = new ByteArrayContent(chunk);
                content.Headers.ContentLength = chunkSize;
                content.Headers.ContentRange = new System.Net.Http.Headers.ContentRangeHeaderValue(position, position + chunkSize - 1, totalLength);

                var response = await httpClient.PutAsync(uploadSession.UploadUrl, content, cancellationToken);

                if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.Accepted)
                {
                    throw new InvalidOperationException($"Chunk upload failed. Status: {response.StatusCode}, Range: {contentRange}");
                }

                position += chunkSize;

                // If this is the last chunk, parse the response to get the DriveItem
                if (position >= totalLength && response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    var uploadedItem = System.Text.Json.JsonSerializer.Deserialize<DriveItem>(responseContent);
                    if (uploadedItem is null)
                    {
                        throw new InvalidOperationException("Failed to deserialize uploaded DriveItem from response");
                    }
                    return uploadedItem;
                }
            }

            throw new InvalidOperationException("Upload completed but no DriveItem was returned");
        }
    }

    /// <inheritdoc/>
    public async Task DeleteFileAsync(string accountId, string itemId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);
        ArgumentNullException.ThrowIfNull(itemId);

        var graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        var drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null)
        {
            throw new InvalidOperationException("Unable to access user's drive");
        }

        // DELETE /drives/{driveId}/items/{itemId}
        await graphClient.Drives[drive.Id].Items[itemId].DeleteAsync(cancellationToken: cancellationToken);
    }
}
