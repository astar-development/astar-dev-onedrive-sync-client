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

    private async Task<GraphServiceClient> CreateGraphClientAsync(CancellationToken cancellationToken)
    {
        // Get the first authenticated account (for now - multi-account support will improve this)
        var accounts = await _authService.GetAuthenticatedAccountsAsync(cancellationToken);
        var firstAccount = accounts.Count > 0 ? accounts[0] : default;
        var accountId = firstAccount.AccountId;

        if (string.IsNullOrEmpty(accountId))
        {
            throw new InvalidOperationException("No authenticated account found");
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
            return token ?? throw new InvalidOperationException("Failed to acquire access token");
        }
    }

    /// <inheritdoc/>
    public async Task<Drive?> GetMyDriveAsync(CancellationToken cancellationToken = default)
    {
        var graphClient = await CreateGraphClientAsync(cancellationToken);
        return await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<DriveItem?> GetDriveRootAsync(CancellationToken cancellationToken = default)
    {
        var graphClient = await CreateGraphClientAsync(cancellationToken);
        var drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Root is null) return null;

        return await graphClient.Drives[drive.Id].Root.GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DriveItem>> GetDriveItemChildrenAsync(string itemId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemId);

        var graphClient = await CreateGraphClientAsync(cancellationToken);
        var drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null) return Enumerable.Empty<DriveItem>();

        var response = await graphClient.Drives[drive.Id].Items[itemId].Children.GetAsync(cancellationToken: cancellationToken);
        return response?.Value ?? Enumerable.Empty<DriveItem>();
    }

    /// <inheritdoc/>
    public async Task<DriveItem?> GetDriveItemAsync(string itemId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(itemId);

        var graphClient = await CreateGraphClientAsync(cancellationToken);
        var drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null) return null;

        return await graphClient.Drives[drive.Id].Items[itemId].GetAsync(cancellationToken: cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DriveItem>> GetRootChildrenAsync(CancellationToken cancellationToken = default)
    {
        var graphClient = await CreateGraphClientAsync(cancellationToken);
        var drive = await graphClient.Me.Drive.GetAsync(cancellationToken: cancellationToken);
        if (drive?.Id is null) return Enumerable.Empty<DriveItem>();

        var root = await graphClient.Drives[drive.Id].Root.GetAsync(cancellationToken: cancellationToken);
        if (root?.Id is null) return Enumerable.Empty<DriveItem>();

        var response = await graphClient.Drives[drive.Id].Items[root.Id].Children.GetAsync(cancellationToken: cancellationToken);
        return response?.Value ?? Enumerable.Empty<DriveItem>();
    }
}
