using AStar.Dev.Functional.Extensions;
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
    public async Task<Result<MsalAuthResult, ErrorResponse>> AcquireTokenInteractiveAsync(IEnumerable<string> scopes, CancellationToken cancellationToken)
        => await Try.RunAsync(() => publicClientApp.AcquireTokenInteractive(scopes).ExecuteAsync(cancellationToken))
            .MapAsync(result => MsalAuthResult.FromMsal(result))
            .MapFailureAsync(ex => new ErrorResponse(ex.GetBaseException().Message));

    /// <inheritdoc />
    public async Task<Result<MsalAuthResult, ErrorResponse>> AcquireTokenSilentAsync(IEnumerable<string> scopes, IAccount account, CancellationToken cancellationToken)
        => await Try.RunAsync(() => publicClientApp.AcquireTokenSilent(scopes, account).ExecuteAsync(cancellationToken))
            .MapAsync(result => MsalAuthResult.FromMsal(result))
            .MapFailureAsync(ex => new ErrorResponse(ex.GetBaseException().Message));

    /// <inheritdoc />
    public async Task<Result<IEnumerable<IAccount>, ErrorResponse>> GetAccountsAsync(CancellationToken cancellationToken)
        => await Try.RunAsync(publicClientApp.GetAccountsAsync)
            .MapFailureAsync(ex => new ErrorResponse(ex.GetBaseException().Message));

    /// <inheritdoc />
    public async Task<Result<Unit, ErrorResponse>> RemoveAsync(IAccount account, CancellationToken cancellationToken)
        => await Try.RunAsync(() => publicClientApp.RemoveAsync(account))
            .MapAsync(_ => Unit.Value)
            .MapFailureAsync(ex => new ErrorResponse(ex.GetBaseException().Message));
}
