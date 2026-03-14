using AStar.Dev.OneDrive.Sync.Client;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.OneDriveServices;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using AStar.Dev.OneDrive.Sync.Client.SyncronisationConflicts;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.IO.Abstractions;
using Testably.Abstractions;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit;

/// <summary>
///     Tests for <see cref="ApplicationServiceExtensions"/>.
/// </summary>
public class ApplicationServiceExtensionsShould
{
    [Fact]
    public void AddApplicationServices_ShouldRegisterIFileSystemAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        provider.GetService<IFileSystem>().ShouldNotBeNull();
        provider.GetService<IFileSystem>().ShouldBeOfType<RealFileSystem>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIFileWatcherServiceAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        provider.GetService<IFileWatcherService>().ShouldNotBeNull();
        provider.GetService<IFileWatcherService>().ShouldBeOfType<FileWatcherService>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIAutoSyncCoordinatorAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        provider.GetService<IAutoSyncCoordinator>().ShouldNotBeNull();
        provider.GetService<IAutoSyncCoordinator>().ShouldBeOfType<AutoSyncCoordinator>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIAutoSyncSchedulerServiceAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();

        // Act & Assert
        provider.GetService<IAutoSyncSchedulerService>().ShouldNotBeNull();
        provider.GetService<IAutoSyncSchedulerService>().ShouldBeOfType<AutoSyncSchedulerService>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIWindowPreferencesServiceAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<IWindowPreferencesService>().ShouldNotBeNull();
        scopedProvider.GetService<IWindowPreferencesService>().ShouldBeOfType<WindowPreferencesService>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIGraphApiClientAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<IGraphApiClient>().ShouldNotBeNull();
        scopedProvider.GetService<IGraphApiClient>().ShouldBeOfType<GraphApiClient>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIFolderTreeServiceAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<IFolderTreeService>().ShouldNotBeNull();
        scopedProvider.GetService<IFolderTreeService>().ShouldBeOfType<FolderTreeService>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterISyncSelectionServiceAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<ISyncSelectionService>().ShouldNotBeNull();
        scopedProvider.GetService<ISyncSelectionService>().ShouldBeOfType<SyncSelectionService>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterILocalFileScannerAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<ILocalFileScanner>().ShouldNotBeNull();
        scopedProvider.GetService<ILocalFileScanner>().ShouldBeOfType<LocalFileScanner>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIRemoteChangeDetectorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<IRemoteChangeDetector>().ShouldNotBeNull();
        scopedProvider.GetService<IRemoteChangeDetector>().ShouldBeOfType<RemoteChangeDetector>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIConflictResolverAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<IConflictResolver>().ShouldNotBeNull();
        scopedProvider.GetService<IConflictResolver>().ShouldBeOfType<ConflictResolver>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIConflictDetectionServiceAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<IConflictDetectionService>().ShouldNotBeNull();
        scopedProvider.GetService<IConflictDetectionService>().ShouldBeOfType<ConflictDetectionService>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIDeletionSyncServiceAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<IDeletionSyncService>().ShouldNotBeNull();
        scopedProvider.GetService<IDeletionSyncService>().ShouldBeOfType<DeletionSyncService>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterISyncEngineAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<ISyncEngine>().ShouldNotBeNull();
        scopedProvider.GetService<ISyncEngine>().ShouldBeOfType<SyncEngine>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIFileTransferServiceAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<IFileTransferService>().ShouldNotBeNull();
        scopedProvider.GetService<IFileTransferService>().ShouldBeOfType<FileTransferService>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIDebugLoggerAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<IDebugLogger>().ShouldNotBeNull();
        scopedProvider.GetService<IDebugLogger>().ShouldBeOfType<DebugLoggerService>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIDeltaPageProcessorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<IDeltaPageProcessor>().ShouldNotBeNull();
        scopedProvider.GetService<IDeltaPageProcessor>().ShouldBeOfType<DeltaPageProcessor>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIDeltaProcessingServiceAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<IDeltaProcessingService>().ShouldNotBeNull();
        scopedProvider.GetService<IDeltaProcessingService>().ShouldBeOfType<DeltaProcessingService>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterISyncRepositoryAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<ISyncRepository>().ShouldNotBeNull();
        scopedProvider.GetService<ISyncRepository>().ShouldBeOfType<EfSyncRepository>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterISyncStateCoordinatorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<ISyncStateCoordinator>().ShouldNotBeNull();
        scopedProvider.GetService<ISyncStateCoordinator>().ShouldBeOfType<SyncStateCoordinator>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIThemeServiceAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<IThemeService>().ShouldNotBeNull();
        scopedProvider.GetService<IThemeService>().ShouldBeOfType<ThemeService>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterIThemeStartupCoordinatorAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<IThemeStartupCoordinator>().ShouldNotBeNull();
        scopedProvider.GetService<IThemeStartupCoordinator>().ShouldBeOfType<ThemeStartupCoordinator>();
    }

    [Fact]
    public void AddApplicationServices_ShouldRegisterSettingsViewModelAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplicationServices();
        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Act & Assert
        scopedProvider.GetService<SettingsViewModel>().ShouldNotBeNull();
        scopedProvider.GetService<SettingsViewModel>().ShouldBeOfType<SettingsViewModel>();
    }
}