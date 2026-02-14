using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication;

/// <summary>
///     Wrapper implementation for <see cref="IPublicClientApplication" /> that delegates to MSAL.
/// </summary>
/// <remarks>
///     This wrapper exists because MSAL's builder classes are sealed and cannot be mocked in tests.
///     By wrapping the interaction with MSAL behind this interface, we can inject mocks for testing.
/// </remarks>
public sealed class AuthenticationClient(IPublicClientApplication publicClientApp) : IAuthenticationClient
{
    /// <inheritdoc />
    public async Task<MsalAuthResult> AcquireTokenInteractiveAsync(IEnumerable<string> scopes, CancellationToken cancellationToken = default)
    {
        Microsoft.Identity.Client.AuthenticationResult result = await publicClientApp
            .AcquireTokenInteractive(scopes)
            .ExecuteAsync(cancellationToken);

        return MsalAuthResult.FromMsal(result);
    }

    /// <inheritdoc />
    public async Task<MsalAuthResult> AcquireTokenSilentAsync(IEnumerable<string> scopes, IAccount account, CancellationToken cancellationToken = default)
    {
        Microsoft.Identity.Client.AuthenticationResult result = await publicClientApp
            .AcquireTokenSilent(scopes, account)
            .ExecuteAsync(cancellationToken);

        return MsalAuthResult.FromMsal(result);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IAccount>> GetAccountsAsync(CancellationToken cancellationToken = default) => await publicClientApp.GetAccountsAsync();

    /// <inheritdoc />
    public async Task RemoveAsync(IAccount account, CancellationToken cancellationToken = default) => await publicClientApp.RemoveAsync(account);
}
