using AStar.Dev.OneDrive.Sync.Client.Infrastructure.GraphApi;
using Microsoft.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.GraphApi;

public class GraphApiClientFactoryShould
{
    private readonly GraphApiClientFactory _factory;

    public GraphApiClientFactoryShould()
    {
        _factory = new GraphApiClientFactory();
    }

    [Fact]
    public void CreateClientWithValidAccessToken()
    {
        string accessToken = "valid-access-token";

        GraphServiceClient client = _factory.CreateClient(accessToken);

        client.ShouldNotBeNull();
    }

    [Fact]
    public void ThrowArgumentExceptionWhenAccessTokenIsNull()
    {
        Should.Throw<ArgumentException>(() => _factory.CreateClient(null!));
    }

    [Fact]
    public void ThrowArgumentExceptionWhenAccessTokenIsEmpty()
    {
        Should.Throw<ArgumentException>(() => _factory.CreateClient(string.Empty));
    }

    [Fact]
    public void CreateNewClientInstanceEachTime()
    {
        string accessToken = "valid-access-token";

        GraphServiceClient client1 = _factory.CreateClient(accessToken);
        GraphServiceClient client2 = _factory.CreateClient(accessToken);

        client1.ShouldNotBeNull();
        client2.ShouldNotBeNull();
        client1.ShouldNotBe(client2);
    }
}
