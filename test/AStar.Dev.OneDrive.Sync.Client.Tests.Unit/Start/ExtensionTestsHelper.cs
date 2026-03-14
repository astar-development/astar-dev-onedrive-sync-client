using AStar.Dev.OneDrive.Sync.Client.Start;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Start;

public static class ExtensionTestsHelper
{
    public static ServiceProvider CreateSystemUnderTest()
    {
        var services = new ServiceCollection();
        services.AddApplicationServices();
        services.AddDatabaseServices();
        IConfigurationRoot configuration = services.AddApplicationConfiguration();
        services.AddAuthenticationServices(configuration);
        services.AddHttpClientWithRetry();
        services.AddViewModels();

        return services.BuildServiceProvider();
    }

    public static IServiceProvider CreateScopedSystemUnderTest()
    {
        ServiceProvider provider = CreateSystemUnderTest();

        IServiceScope scope = provider.CreateScope();

        return scope.ServiceProvider;
    }
}