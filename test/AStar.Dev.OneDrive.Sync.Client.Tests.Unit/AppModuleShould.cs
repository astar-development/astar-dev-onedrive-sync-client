using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Configuration;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Resilience;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit;

public class AppModuleShould
{
    [Fact]
    public void RegisterServicesWithValidConfiguration()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = BuildTestConfiguration();

        services.AddAppModule(configuration);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        serviceProvider.ShouldNotBeNull();
    }

    [Fact]
    public void RegisterAuthenticationOptionsWithCorrectValues()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = BuildTestConfiguration();

        services.AddAppModule(configuration);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        AuthenticationOptions authOptions = serviceProvider.GetRequiredService<AuthenticationOptions>();
        authOptions.ShouldNotBeNull();
        authOptions.Microsoft.ClientId.ShouldBe("test-client-id");
        authOptions.Microsoft.TenantId.ShouldBe("test-tenant-id");
        authOptions.Microsoft.RedirectUri.ShouldBe("http://localhost");
    }

    [Fact]
    public void RegisterSyncOptionsWithCorrectValues()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = BuildTestConfiguration();

        services.AddAppModule(configuration);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        SyncOptions syncOptions = serviceProvider.GetRequiredService<SyncOptions>();
        syncOptions.ShouldNotBeNull();
        syncOptions.DefaultConcurrentUploads.ShouldBe(5);
        syncOptions.DefaultConcurrentDownloads.ShouldBe(5);
    }

    [Fact]
    public void RegisterTelemetryOptionsWithCorrectValues()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = BuildTestConfiguration();

        services.AddAppModule(configuration);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        TelemetryOptions telemetryOptions = serviceProvider.GetRequiredService<TelemetryOptions>();
        telemetryOptions.ShouldNotBeNull();
        telemetryOptions.Enabled.ShouldBe(true);
    }

    [Fact]
    public void RegisterOptionsPatternForAuthenticationOptions()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = BuildTestConfiguration();

        services.AddAppModule(configuration);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        IOptions<AuthenticationOptions> options = serviceProvider.GetRequiredService<IOptions<AuthenticationOptions>>();
        options.Value.Microsoft.ClientId.ShouldBe("test-client-id");
    }

    [Fact]
    public void RegisterResiliencePolicyFactoryAsSingleton()
    {
        var services = new ServiceCollection();
        IConfiguration configuration = BuildTestConfiguration();

        services.AddAppModule(configuration);
        ServiceProvider serviceProvider = services.BuildServiceProvider();

        ResiliencePolicyFactory factory1 = serviceProvider.GetRequiredService<ResiliencePolicyFactory>();
        ResiliencePolicyFactory factory2 = serviceProvider.GetRequiredService<ResiliencePolicyFactory>();

        factory1.ShouldNotBeNull();
        factory1.ShouldBe(factory2);
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
