using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace AStarOneDriveClient.Authentication;

/// <summary>
/// Service for managing Microsoft authentication via MSAL.
/// </summary>
public sealed class AuthService : IAuthService
{
    private readonly IAuthenticationClient _authClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthService"/> class.
    /// </summary>
    /// <param name="authClient">The authentication client wrapper.</param>
    public AuthService(IAuthenticationClient authClient)
    {
        ArgumentNullException.ThrowIfNull(authClient);
        _authClient = authClient;
    }

    /// <summary>
    /// Creates a new AuthService with default MSAL configuration.
    /// </summary>
    /// <returns>Configured AuthService instance.</returns>
    public static async Task<AuthService> CreateAsync()
    {
        var app = PublicClientApplicationBuilder
            .Create(AuthConfiguration.ClientId)
            .WithAuthority(AuthConfiguration.Authority)
            .WithRedirectUri(AuthConfiguration.RedirectUri)
            .Build();

        // Setup token cache persistence
        var storageProperties = new StorageCreationPropertiesBuilder(
            "astar_onedrive_cache.dat",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AStarOneDriveClient"))
            .Build();

        var cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(app.UserTokenCache);

        return new AuthService(new AuthenticationClient(app));
    }

    /// <inheritdoc/>
    public async Task<AuthenticationResult> LoginAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _authClient
                .AcquireTokenInteractiveAsync(AuthConfiguration.Scopes, cancellationToken);

            return new AuthenticationResult(
                Success: true,
                AccountId: result.Account.HomeAccountId.Identifier,
                DisplayName: result.Account.Username,
                ErrorMessage: null
            );
        }
        catch (MsalException ex)
        {
            return new AuthenticationResult(
                Success: false,
                AccountId: null,
                DisplayName: null,
                ErrorMessage: ex.Message
            );
        }
        catch (OperationCanceledException)
        {
            return new AuthenticationResult(
                Success: false,
                AccountId: null,
                DisplayName: null,
                ErrorMessage: "Login was cancelled."
            );
        }
    }

    /// <inheritdoc/>
    public async Task<bool> LogoutAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        try
        {
            var accounts = await _authClient.GetAccountsAsync();
            var account = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);

            if (account is not null)
            {
                await _authClient.RemoveAsync(account);
                return true;
            }

            return false;
        }
        catch (MsalException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<(string AccountId, string DisplayName)>> GetAuthenticatedAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var accounts = await _authClient.GetAccountsAsync();
            return accounts
                .Select(a => (a.HomeAccountId.Identifier, a.Username))
                .ToList();
        }
        catch (MsalException)
        {
            return Array.Empty<(string, string)>();
        }
    }

    /// <inheritdoc/>
    public async Task<string?> GetAccessTokenAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        try
        {
            var accounts = await _authClient.GetAccountsAsync();
            var account = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);

            if (account is null)
            {
                return null;
            }

            var result = await _authClient
                .AcquireTokenSilentAsync(AuthConfiguration.Scopes, account, cancellationToken);

            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            return null;
        }
        catch (MsalException)
        {
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> IsAuthenticatedAsync(string accountId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accountId);

        try
        {
            var accounts = await _authClient.GetAccountsAsync();
            return accounts.Any(a => a.HomeAccountId.Identifier == accountId);
        }
        catch (MsalException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<string?> AcquireTokenSilentAsync(string accountId, CancellationToken cancellationToken = default)
    {
        return await GetAccessTokenAsync(accountId, cancellationToken);
    }
}
