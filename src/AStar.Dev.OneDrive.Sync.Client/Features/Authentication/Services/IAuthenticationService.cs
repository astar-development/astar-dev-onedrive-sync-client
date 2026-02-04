
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;
/// <summary>
/// Service for handling OAuth authentication with Microsoft using Device Code Flow.
/// Authenticates users and manages token lifecycle.
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Authenticates a user using Device Code Flow with a 30-second timeout.
    /// During the flow, the user is directed to authenticate in a browser.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the authentication operation.</param>
    /// <returns>
    /// A result containing the authenticated token if successful,
    /// or an AuthenticationError if the operation fails or times out.
    /// </returns>
    Task<Result<AuthToken, AuthenticationError>> AuthenticateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired or expiring access token using silent authentication.
    /// MSAL caches refresh tokens internally and handles the refresh automatically.
    /// Implements exponential backoff for transient failures.
    /// </summary>
    /// <param name="accountIdentifier">The account identifier (email or username) to refresh.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A result containing the refreshed token if successful,
    /// or an AuthenticationError if refresh fails.
    /// </returns>
    Task<Result<AuthToken, AuthenticationError>> RefreshTokenAsync(string accountIdentifier, CancellationToken cancellationToken = default);
}
