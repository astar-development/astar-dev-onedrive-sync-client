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
            System.Diagnostics.Debug.WriteLine($"[GraphTokenProvider] Getting token for account: {_accountId}");
            var token = await _authService.GetAccessTokenAsync(_accountId, cancellationToken);
            if (token is null)
            {
                System.Diagnostics.Debug.WriteLine($"[GraphTokenProvider] ERROR: Token is null for account: {_accountId}");
                throw new InvalidOperationException($"Failed to acquire access token for account: {_accountId}");
            }
            System.Diagnostics.Debug.WriteLine($"[GraphTokenProvider] Token acquired successfully for account: {_accountId}");
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
        try
        {
            System.Diagnostics.Debug.WriteLine($"[GraphApiClient] GetDriveRootAsync called for account: {accountId}");
            var graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
            System.Diagnostics.Debug.WriteLine($"[GraphApiClient] GraphClient created, getting drive...");
            var drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
            System.Diagnostics.Debug.WriteLine($"[GraphApiClient] Drive retrieved: {drive?.Id}");
            if (drive?.Id is null)
            {
                System.Diagnostics.Debug.WriteLine($"[GraphApiClient] Drive ID is null");
                return null;
            }

            var root = await graphClient.Drives[drive.Id].Root.GetAsync(cancellationToken: cancellationToken);
            System.Diagnostics.Debug.WriteLine($"[GraphApiClient] Root item retrieved: {root?.Name}, ID: {root?.Id}");
            return root;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GraphApiClient] ERROR in GetDriveRootAsync: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DriveItem>> GetDriveItemChildrenAsync(string accountId, string itemId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemId);

        var graphClient = await CreateGraphClientAsync(accountId, cancellationToken);
        var drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null) return Enumerable.Empty<DriveItem>();

        var response = await graphClient.Drives[drive.Id].Items[itemId].Children.GetAsync(cancellationToken: cancellationToken);
        return response?.Value ?? Enumerable.Empty<DriveItem>();
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
}
