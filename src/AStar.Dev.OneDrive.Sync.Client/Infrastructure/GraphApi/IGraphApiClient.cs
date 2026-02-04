using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.GraphApi;

/// <summary>
/// Interface for Microsoft Graph API client operations.
/// Provides methods for user profile retrieval and other Graph API interactions.
/// </summary>
/// <remarks>
/// This interface is designed to be implemented using Kiota V5 generated clients
/// or can be manually implemented for testing and custom scenarios.
/// </remarks>
public interface IGraphApiClient
{
    /// <summary>
    /// Retrieves the current user's profile information from Microsoft Graph API.
    /// </summary>
    /// <param name=\"authToken\">The authenticated OAuth token for API access.</param>
    /// <param name=\"cancellationToken\">Allows cancellation of the operation.</param>
    /// <returns>The user's profile containing email and account ID.</returns>
    /// <exception cref=\"ArgumentNullException\">Thrown when authToken is null.</exception>
    /// <exception cref=\"HttpRequestException\">Thrown when the Graph API call fails.</exception>
    Task<UserProfile> GetUserProfileAsync(AuthToken authToken, CancellationToken cancellationToken = default);
}
