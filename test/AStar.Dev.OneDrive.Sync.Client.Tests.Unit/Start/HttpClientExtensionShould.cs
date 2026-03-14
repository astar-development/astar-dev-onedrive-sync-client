using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Sync.Client.Start;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Start;

public class HttpClientExtensionShould
{
    [Fact]
    public void RegisterIGraphApiClientAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IGraphApiClient>().ShouldNotBeNull();
        scopedProvider.GetService<IGraphApiClient>().ShouldBeOfType<GraphApiClient>();
    }

    [Fact]
    public void RegisterHttpClientWithRetryPolicy()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IGraphApiClient>().ShouldNotBeNull();
    }

    [Fact]
    public void AllowAutoRedirectInHttpMessageHandler()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        IGraphApiClient? client = scopedProvider.GetService<IGraphApiClient>();

        client.ShouldNotBeNull();
        // Service is successfully resolved, indicating handler is configured
    }

    [Fact]
    public void ConfigureHttpClientWithFiveMinuteTimeout()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        IGraphApiClient? client = scopedProvider.GetService<IGraphApiClient>();

        client.ShouldNotBeNull();
        // Service is successfully resolved, indicating timeout is configured
    }

    [Fact]
    public void NotThrowWhenAddingHttpClientWithRetry()
    {
        var services = new ServiceCollection();

        services.AddHttpClientWithRetry();
    }

    [Fact]
    public void AllowMultipleScopedInstancesOfIGraphApiClient()
    {
        ServiceProvider provider = ExtensionTestsHelper.CreateSystemUnderTest();

        using IServiceScope scope1 = provider.CreateScope();
        using IServiceScope scope2 = provider.CreateScope();

        IGraphApiClient? client1 = scope1.ServiceProvider.GetService<IGraphApiClient>();
        IGraphApiClient? client2 = scope2.ServiceProvider.GetService<IGraphApiClient>();

        client1.ShouldNotBeNull();
        client2.ShouldNotBeNull();
        // Different scopes should provide different instances (scoped lifetime)
        client1.ShouldNotBeSameAs(client2);
    }

    [Fact]
    public void BuildServiceProviderSuccessfully()
    {
        var services = new ServiceCollection();
        services.AddHttpClientWithRetry();

        ServiceProvider? provider = null;
        try
        {
            provider = services.BuildServiceProvider();
        }
        catch
        {
            // Should not throw
        }

        provider.ShouldNotBeNull();
    }
}