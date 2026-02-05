using Microsoft.Graph;
using Azure.Core;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.GraphApi;

/// <summary>
/// Factory for creating authenticated Microsoft Graph API client instances.
/// Handles GraphServiceClient instantiation with proper authentication provider configuration.
/// </summary>
public class GraphApiClientFactory
{
    /// <summary>
    /// Creates a new GraphServiceClient with token-based authentication.
    /// </summary>
    /// <param name="accessToken">The OAuth access token for authentication.</param>
    /// <returns>A configured GraphServiceClient instance.</returns>
    /// <exception cref="ArgumentException">Thrown when accessToken is null or empty.</exception>
    public GraphServiceClient CreateClient(string accessToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(accessToken, nameof(accessToken));

        // Create token credential from access token
        var tokenCredential = new StaticTokenCredential(accessToken);
        
        // Create GraphServiceClient with the token credential
        var graphClient = new GraphServiceClient(tokenCredential);
        
        return graphClient;
    }
    
    /// <summary>
    /// Simple token credential that provides a static access token.
    /// </summary>
    private class StaticTokenCredential : TokenCredential
    {
        private readonly string _accessToken;

        public StaticTokenCredential(string accessToken)
        {
            _accessToken = accessToken;
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(_accessToken, DateTimeOffset.UtcNow.AddHours(1));
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(new AccessToken(_accessToken, DateTimeOffset.UtcNow.AddHours(1)));
        }
    }
}
