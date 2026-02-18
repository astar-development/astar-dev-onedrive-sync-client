using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Core;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication;

/// <summary>
///     Service for managing Microsoft authentication via MSAL.
/// </summary>
public sealed class AuthService(IAuthenticationClient authClient, AuthConfiguration configuration) : IAuthService
{
    /// <inheritdoc />
    public async Task<Result<AuthenticationResult, ErrorResponse>> LoginAsync(CancellationToken cancellationToken)
        => await authClient.AcquireTokenInteractiveAsync(configuration.Scopes, cancellationToken)
            .MatchAsync(
                success => new Result<AuthenticationResult, ErrorResponse>.Ok(AuthenticationResult.Success(success.Account.HomeAccountId.Identifier, new Core.Models.HashedAccountId(AccountIdHasher.Hash(success.Account.HomeAccountId.Identifier)), success.Account.Username)),
                error => {var message = cancellationToken.IsCancellationRequested
                ? "Login was cancelled."
                : "Login timed out after 30 seconds.";

            return new Result<AuthenticationResult, ErrorResponse>.Error(new ErrorResponse(message));
            });

    /// <inheritdoc />
    public async Task<Result<bool, ErrorResponse>> LogoutAsync(string accountId, CancellationToken cancellationToken = default)
    {
        Result<IEnumerable<IAccount>, ErrorResponse> accounts = await authClient.GetAccountsAsync(cancellationToken);
        return await accounts.MatchAsync<Result<bool, ErrorResponse>>(
            async accountsList =>
            {
                IAccount? account = accountsList.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);
                if (account is not null)
                {
                    _ = await authClient.RemoveAsync(account, cancellationToken);
                    return new Result<bool, ErrorResponse>.Ok(true);
                }

                return new Result<bool, ErrorResponse>.Ok(false);
            },
            error => Task.FromResult<Result<bool, ErrorResponse>>(new Result<bool, ErrorResponse>.Ok(false))
        );
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<(string accountId, string DisplayName)>, ErrorResponse>> GetAuthenticatedAccountsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            Result<IEnumerable<IAccount>, ErrorResponse> accounts = await authClient.GetAccountsAsync(cancellationToken);
            return accounts.Match(
                accountsList => new Result<IReadOnlyList<(string, string)>, ErrorResponse>.Ok(accountsList.Select(a => (a.HomeAccountId.Identifier, a.Username)).ToList()),
                error => new Result<IReadOnlyList<(string, string)>, ErrorResponse>.Ok(new List<(string, string)>()));
        }
        catch(MsalException)
        {
            return new Result<IReadOnlyList<(string, string)>, ErrorResponse>.Ok(new List<(string, string)>());
        }
    }

    /// <inheritdoc />
    public async Task<Result<string?, ErrorResponse>> GetAccessTokenAsync(string accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            Result<IEnumerable<IAccount>, ErrorResponse> accounts = await authClient.GetAccountsAsync(cancellationToken);
            IAccount? account = accounts.Match(
                accountsList => accountsList.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId),
                error => null);

            if(account is null)
                return new Result<string?, ErrorResponse>.Ok(null);

            Result<MsalAuthResult, ErrorResponse> result = await authClient
                .AcquireTokenSilentAsync(configuration.Scopes, account, cancellationToken);

            return result.Match(
                success => new Result<string?, ErrorResponse>.Ok(success.AccessToken),
                error => new Result<string?, ErrorResponse>.Ok(null));
        }
        catch(MsalUiRequiredException)
        {
            return new Result<string?, ErrorResponse>.Ok(null);
        }
        catch(MsalException)
        {
            return new Result<string?, ErrorResponse>.Ok(null);
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool, ErrorResponse>> IsAuthenticatedAsync(string accountId, CancellationToken cancellationToken = default)
    {
        try
        {
            Result<IEnumerable<IAccount>, ErrorResponse> accounts = await authClient.GetAccountsAsync(cancellationToken);
            IAccount? account = accounts.Match(
                accountsList => accountsList.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId),
                error => null);

            return new Result<bool, ErrorResponse>.Ok(account is not null);
        }
        catch(MsalException)
        {
            return new Result<bool, ErrorResponse>.Ok(false);
        }
    }

    /// <inheritdoc />
    public async Task<Result<string?, ErrorResponse>> AcquireTokenSilentAsync(string accountId, CancellationToken cancellationToken = default) => await GetAccessTokenAsync(accountId, cancellationToken);

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
            ApplicationMetadata.ApplicationFolder);

        var storagePropertiesBuilder = new StorageCreationPropertiesBuilder(
            "astar-onedrive-sync-cache.dat",
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
