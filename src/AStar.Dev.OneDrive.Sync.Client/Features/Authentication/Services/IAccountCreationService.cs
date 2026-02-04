using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;

/// <summary>
/// Service for orchestrating account creation after successful authentication.
/// </summary>
public interface IAccountCreationService
{
    /// <summary>
    /// Creates a new account using authenticated credentials.
    /// Handles the complete workflow from Graph API profile retrieval to database persistence.
    /// </summary>
    /// <param name=\"authToken\">The authenticated OAuth token from AuthenticationService.</param>
    /// <param name=\"cancellationToken\">Allows cancellation of the operation.</param>
    /// <returns>
    /// Success: A Result containing the newly created Account entity.
    /// Failure: A Result containing an AccountCreationError indicating what went wrong.
    /// </returns>
    /// <remarks>
    /// This method ensures GDPR compliance by:
    /// - Hashing email and account ID before storage
    /// - Checking for duplicate accounts before creation
    /// - Storing tokens securely with hashed identifiers
    /// </remarks>
    Task<Result<Account, AccountCreationError>> CreateAccountAsync(AuthToken authToken, CancellationToken cancellationToken = default);
}
