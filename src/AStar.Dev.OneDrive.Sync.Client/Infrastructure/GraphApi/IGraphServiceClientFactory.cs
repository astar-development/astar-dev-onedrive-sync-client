using Microsoft.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.GraphApi;

public interface IGraphServiceClientFactory
{
    GraphServiceClient CreateClient(string accessToken);
}
