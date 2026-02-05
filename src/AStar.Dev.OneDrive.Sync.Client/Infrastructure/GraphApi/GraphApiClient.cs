using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.GraphApi;

/// <summary>
/// Microsoft Graph SDK implementation of IGraphApiClient.
/// Wraps GraphServiceClient to provide authenticated access to Microsoft Graph API.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GraphApiClient"/> class.
/// </remarks>
/// <param name="factory">Factory for creating GraphServiceClient instances.</param>
public class GraphApiClient(GraphApiClientFactory factory) : IGraphApiClient
{
    private readonly GraphApiClientFactory _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    /// <inheritdoc/>
    public async Task<UserProfile> GetUserProfileAsync(AuthToken authToken, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(authToken);

        GraphServiceClient client = _factory.CreateClient(authToken.AccessToken);
        
        User? user = await client.Me.GetAsync(cancellationToken: cancellationToken);

        return user is null || string.IsNullOrEmpty(user.Mail) || string.IsNullOrEmpty(user.Id)
            ? throw new InvalidOperationException("Graph API returned incomplete user profile data")
            : new UserProfile(Email: user.Mail, AccountId: user.Id);
    }
}
