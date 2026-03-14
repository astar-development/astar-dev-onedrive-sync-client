using AStar.Dev.OneDrive.Sync.Client.Core.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace AStar.Dev.OneDrive.Sync.Client.Start;

public class DatabaseServiceExtensionsShould
{
    [Fact]
    public void RegisterDbContextFactory()
    {
        var services = new ServiceCollection();
        services.AddDatabaseServices();

        ServiceDescriptor? descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IDbContextFactory<SyncDbContext>));

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Singleton);
    }

    [Fact]
    public void RegisterDbContext()
    {
        var services = new ServiceCollection();
        services.AddDatabaseServices();

        ServiceDescriptor? descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(SyncDbContext));

        descriptor.ShouldNotBeNull();
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void RegisterSyncRepository()
    {
        var services = new ServiceCollection();
        services.AddDatabaseServices();

        ServiceDescriptor? descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ISyncRepository));

        descriptor.ShouldNotBeNull();
        descriptor.ImplementationType.ShouldBe(typeof(EfSyncRepository));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void RegisterAccountRepository()
    {
        var services = new ServiceCollection();
        services.AddDatabaseServices();

        ServiceDescriptor? descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IAccountRepository));

        descriptor.ShouldNotBeNull();
        descriptor.ImplementationType.ShouldBe(typeof(AccountRepository));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void RegisterSyncConfigurationRepository()
    {
        var services = new ServiceCollection();
        services.AddDatabaseServices();

        ServiceDescriptor? descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ISyncConfigurationRepository));

        descriptor.ShouldNotBeNull();
        descriptor.ImplementationType.ShouldBe(typeof(SyncConfigurationRepository));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void RegisterDriveItemsRepository()
    {
        var services = new ServiceCollection();
        services.AddDatabaseServices();

        ServiceDescriptor? descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IDriveItemsRepository));

        descriptor.ShouldNotBeNull();
        descriptor.ImplementationType.ShouldBe(typeof(DriveItemsRepository));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void RegisterSyncConflictRepository()
    {
        var services = new ServiceCollection();
        services.AddDatabaseServices();

        ServiceDescriptor? descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ISyncConflictRepository));

        descriptor.ShouldNotBeNull();
        descriptor.ImplementationType.ShouldBe(typeof(SyncConflictRepository));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void RegisterSyncSessionLogRepository()
    {
        var services = new ServiceCollection();
        services.AddDatabaseServices();

        ServiceDescriptor? descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(ISyncSessionLogRepository));

        descriptor.ShouldNotBeNull();
        descriptor.ImplementationType.ShouldBe(typeof(SyncSessionLogRepository));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void RegisterFileOperationLogRepository()
    {
        var services = new ServiceCollection();
        services.AddDatabaseServices();

        ServiceDescriptor? descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IFileOperationLogRepository));

        descriptor.ShouldNotBeNull();
        descriptor.ImplementationType.ShouldBe(typeof(FileOperationLogRepository));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void RegisterDebugLogRepository()
    {
        var services = new ServiceCollection();
        services.AddDatabaseServices();

        ServiceDescriptor? descriptor = services.FirstOrDefault(sd => sd.ServiceType == typeof(IDebugLogRepository));

        descriptor.ShouldNotBeNull();
        descriptor.ImplementationType.ShouldBe(typeof(DebugLogRepository));
        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }
}
