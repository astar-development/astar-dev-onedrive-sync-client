using AStar.Dev.OneDrive.Sync.Client;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit;

public class AppModuleShould
{
    [Fact]
    public void RegisterServicesWithValidConfiguration()
    {
        var services = new ServiceCollection();
        var configuration = BuildTestConfiguration();

        services.AddAppModule(configuration);
        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.ShouldNotBeNull();
    }

    [Fact]
    public void RegisterConfigurationOptions()
    {
        var services = new ServiceCollection();
        var configuration = BuildTestConfiguration();

        services.AddAppModule(configuration);
        var serviceProvider = services.BuildServiceProvider();

        var authOptions = serviceProvider.GetService<AuthenticationOptions>();
        authOptions.ShouldNotBeNull();
    }

    [Fact]
    public void ThrowWhenConfigurationIsNull()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() => services.AddAppModule(null!));
    }

    private static IConfiguration BuildTestConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string>
        {
            {"Authentication:Microsoft:ClientId", "test-client-id"},
            {"Authentication:Microsoft:TenantId", "test-tenant-id"},
            {"Authentication:Microsoft:RedirectUri", "http://localhost"},
            {"Sync:DefaultConcurrentUploads", "5"},
            {"Sync:DefaultConcurrentDownloads", "5"},
            {"Telemetry:Enabled", "true"}
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();
    }
}