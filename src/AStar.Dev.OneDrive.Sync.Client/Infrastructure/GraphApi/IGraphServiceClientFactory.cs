using Microsoft.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.GraphApi;

/// <summary>
/// Factory interface for creating authenticated Microsoft Graph API client instances.
/// Abstracts the instantiation of GraphServiceClient with proper authentication provider configuration.
/// </summary>
public interface IGraphServiceClientFactory
{
    /// <summary>
    /// Creates a new GraphServiceClient with token-based authentication.   
    /// </summary>
    /// <param name="accessToken">The access token for authentication.</param>
    /// <returns>A configured GraphServiceClient instance.</returns>
    GraphServiceClient CreateClient(string accessToken);
}
