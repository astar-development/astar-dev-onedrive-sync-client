using AStar.Dev.Functional.Extensions;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication;

/// <summary>
///     Service for managing Microsoft authentication via MSAL.
/// </summary>
public interface IAuthService
{
    /// <summary>
    ///     Initiates interactive login flow for a new account.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Authentication result containing account information.</returns>
    Task<Result<AuthenticationResult, ErrorResponse>> LoginAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Logs out an account by removing it from the token cache.
    /// </summary>
    /// <param name="accountId">The account identifier to log out.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if logout was successful, false otherwise.</returns>
    Task<Result<bool, ErrorResponse>> LogoutAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Gets all authenticated accounts from the token cache.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of account identifiers and display names.</returns>
    Task<Result<IReadOnlyList<(string accountId, string DisplayName)>, ErrorResponse>> GetAuthenticatedAccountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Acquires an access token for Microsoft Graph API calls.
    /// </summary>
    /// <param name="accountId">The account identifier to get a token for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access token if successful, null otherwise.</returns>
    Task<Result<string?, ErrorResponse>> GetAccessTokenAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Checks if an account is currently authenticated.
    /// </summary>
    /// <param name="accountId">The account identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the account is authenticated, false otherwise.</returns>
    Task<Result<bool, ErrorResponse>> IsAuthenticatedAsync(string accountId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Acquires a token silently (without user interaction) if possible.
    /// </summary>
    /// <param name="accountId">The account identifier to get a token for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Access token if successful, null if user interaction is required.</returns>
    Task<Result<string?, ErrorResponse>> AcquireTokenSilentAsync(string accountId, CancellationToken cancellationToken = default);
}
