using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services.Authentication;

/// <summary>
///     Service for managing Microsoft authentication via MSAL.
/// </summary>
public sealed class AuthService(IAuthenticationClient authClient, AuthConfiguration configuration) : IAuthService
{
    /// <inheritdoc />
    public async Task<AuthenticationResult> LoginAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            MsalAuthResult result = await authClient
                .AcquireTokenInteractiveAsync(configuration.Scopes, cts.Token);

            return new AuthenticationResult(
                true,
                result.Account.HomeAccountId.Identifier,
                result.Account.Username,
                null
            );
        }
        catch(MsalException ex)
        {
            return new AuthenticationResult(
                false,
                null,
                null,
                ex.Message
            );
        }
        catch(OperationCanceledException)
        {
            var message = cancellationToken.IsCancellationRequested
                ? "Login was cancelled."
                : "Login timed out after 30 seconds.";

            return new AuthenticationResult(
                false,
                null,
                null,
                message
            );
        }
    }

    /// <inheritdoc />
    public async Task<bool> LogoutAsync(string accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            IEnumerable<IAccount> accounts = await authClient.GetAccountsAsync(cancellationToken);
            IAccount? account = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);

            if(account is not null)
            {
                await authClient.RemoveAsync(account, cancellationToken);
                return true;
            }

            return false;
        }
        catch(MsalException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(string AccountId, string DisplayName)>> GetAuthenticatedAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            IEnumerable<IAccount> accounts = await authClient.GetAccountsAsync(cancellationToken);
            return accounts
                .Select(a => (a.HomeAccountId.Identifier, a.Username))
                .ToList();
        }
        catch(MsalException)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            IEnumerable<IAccount> accounts = await authClient.GetAccountsAsync(cancellationToken);
            IAccount? account = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);

            if(account is null)
                return null;

            MsalAuthResult result = await authClient
                .AcquireTokenSilentAsync(configuration.Scopes, account, cancellationToken);

            return result.AccessToken;
        }
        catch(MsalUiRequiredException)
        {
            return null;
        }
        catch(MsalException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAuthenticatedAsync(string accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            IEnumerable<IAccount> accounts = await authClient.GetAccountsAsync(cancellationToken);
            return accounts.Any(a => a.HomeAccountId.Identifier == accountId);
        }
        catch(MsalException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string?> AcquireTokenSilentAsync(string accountId, CancellationToken cancellationToken = default) => await GetAccessTokenAsync(accountId, cancellationToken);

    /// <summary>
    ///     Creates a new AuthService with default MSAL configuration.
    /// </summary>
    /// <param name="configuration">Authentication configuration.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>Configured AuthService instance.</returns>
    public static async Task<AuthService> CreateAsync(AuthConfiguration configuration)
    {
        IPublicClientApplication app = PublicClientApplicationBuilder
            .Create(configuration.ClientId)
            .WithAuthority(configuration.Authority)
            .WithRedirectUri(configuration.RedirectUri)
            .Build();

        // Setup token cache persistence
        var cacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "astar-dev-onedrive-client");

        var storagePropertiesBuilder = new StorageCreationPropertiesBuilder(
            "astar-onedrive-cache.dat",
            cacheDirectory);

        // Use plaintext storage on Linux due to keyring/libsecret compatibility issues
        // Windows and macOS use platform-specific secure storage (DPAPI and Keychain)
        if(OperatingSystem.IsLinux())
            _ = storagePropertiesBuilder.WithUnprotectedFile();

        StorageCreationProperties storageProperties = storagePropertiesBuilder.Build();
        MsalCacheHelper cacheHelper = await MsalCacheHelper.CreateAsync(storageProperties);
        cacheHelper.RegisterCache(app.UserTokenCache);

        return new AuthService(new AuthenticationClient(app), configuration);
    }
}
