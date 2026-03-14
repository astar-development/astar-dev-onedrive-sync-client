using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.OneDriveServices;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using AStar.Dev.OneDrive.Sync.Client.Start;
using AStar.Dev.OneDrive.Sync.Client.SyncronisationConflicts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System.IO.Abstractions;
using Testably.Abstractions;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Start;

public class ApplicationServiceExtensionsShould
{
    [Fact]
    public void RegisterIFileSystemAsSingleton()
    {
        ServiceProvider provider = ExtensionTestsHelper.CreateSystemUnderTest();

        provider.GetService<IFileSystem>().ShouldNotBeNull();
        provider.GetService<IFileSystem>().ShouldBeOfType<RealFileSystem>();
    }

    [Fact]
    public void RegisterIFileWatcherServiceAsSingleton()
    {
        ServiceProvider provider = ExtensionTestsHelper.CreateSystemUnderTest();

        provider.GetService<IFileWatcherService>().ShouldNotBeNull();
        provider.GetService<IFileWatcherService>().ShouldBeOfType<FileWatcherService>();
    }

    [Fact]
    public void RegisterIAutoSyncCoordinatorAsSingleton()
    {
        ServiceProvider provider = ExtensionTestsHelper.CreateSystemUnderTest();

        provider.GetService<IAutoSyncCoordinator>().ShouldNotBeNull();
        provider.GetService<IAutoSyncCoordinator>().ShouldBeOfType<AutoSyncCoordinator>();
    }

    [Fact]
    public void RegisterIAutoSyncSchedulerServiceAsSingleton()
    {
        ServiceProvider provider = ExtensionTestsHelper.CreateSystemUnderTest();

        provider.GetService<IAutoSyncSchedulerService>().ShouldNotBeNull();
        provider.GetService<IAutoSyncSchedulerService>().ShouldBeOfType<AutoSyncSchedulerService>();
    }

    [Fact]
    public void RegisterIWindowPreferencesServiceAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IWindowPreferencesService>().ShouldNotBeNull();
        scopedProvider.GetService<IWindowPreferencesService>().ShouldBeOfType<WindowPreferencesService>();
    }

    [Fact]
    public void RegisterIGraphApiClientAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IGraphApiClient>().ShouldNotBeNull();
        scopedProvider.GetService<IGraphApiClient>().ShouldBeOfType<GraphApiClient>();
    }

    [Fact]
    public void RegisterIFolderTreeServiceAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IFolderTreeService>().ShouldNotBeNull();
        scopedProvider.GetService<IFolderTreeService>().ShouldBeOfType<FolderTreeService>();
    }

    [Fact]
    public void RegisterISyncSelectionServiceAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<ISyncSelectionService>().ShouldNotBeNull();
        scopedProvider.GetService<ISyncSelectionService>().ShouldBeOfType<SyncSelectionService>();
    }

    [Fact]
    public void RegisterILocalFileScannerAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<ILocalFileScanner>().ShouldNotBeNull();
        scopedProvider.GetService<ILocalFileScanner>().ShouldBeOfType<LocalFileScanner>();
    }

    [Fact]
    public void RegisterIRemoteChangeDetectorAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IRemoteChangeDetector>().ShouldNotBeNull();
        scopedProvider.GetService<IRemoteChangeDetector>().ShouldBeOfType<RemoteChangeDetector>();
    }

    [Fact]
    public void RegisterIConflictResolverAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IConflictResolver>().ShouldNotBeNull();
        scopedProvider.GetService<IConflictResolver>().ShouldBeOfType<ConflictResolver>();
    }

    [Fact]
    public void RegisterIConflictDetectionServiceAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IConflictDetectionService>().ShouldNotBeNull();
        scopedProvider.GetService<IConflictDetectionService>().ShouldBeOfType<ConflictDetectionService>();
    }

    [Fact]
    public void RegisterIDeletionSyncServiceAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IDeletionSyncService>().ShouldNotBeNull();
        scopedProvider.GetService<IDeletionSyncService>().ShouldBeOfType<DeletionSyncService>();
    }

    [Fact]
    public void RegisterISyncEngineAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<ISyncEngine>().ShouldNotBeNull();
        scopedProvider.GetService<ISyncEngine>().ShouldBeOfType<SyncEngine>();
    }

    [Fact]
    public void RegisterIFileTransferServiceAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IFileTransferService>().ShouldNotBeNull();
        scopedProvider.GetService<IFileTransferService>().ShouldBeOfType<FileTransferService>();
    }

    [Fact]
    public void RegisterIDebugLoggerAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IDebugLogger>().ShouldNotBeNull();
        scopedProvider.GetService<IDebugLogger>().ShouldBeOfType<DebugLoggerService>();
    }

    [Fact]
    public void RegisterIDeltaPageProcessorAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IDeltaPageProcessor>().ShouldNotBeNull();
        scopedProvider.GetService<IDeltaPageProcessor>().ShouldBeOfType<DeltaPageProcessor>();
    }

    [Fact]
    public void RegisterIDeltaProcessingServiceAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IDeltaProcessingService>().ShouldNotBeNull();
        scopedProvider.GetService<IDeltaProcessingService>().ShouldBeOfType<DeltaProcessingService>();
    }

    [Fact]
    public void RegisterISyncRepositoryAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<ISyncRepository>().ShouldNotBeNull();
        scopedProvider.GetService<ISyncRepository>().ShouldBeOfType<EfSyncRepository>();
    }

    [Fact]
    public void RegisterISyncStateCoordinatorAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<ISyncStateCoordinator>().ShouldNotBeNull();
        scopedProvider.GetService<ISyncStateCoordinator>().ShouldBeOfType<SyncStateCoordinator>();
    }

    [Fact]
    public void RegisterIThemeServiceAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IThemeService>().ShouldNotBeNull();
        scopedProvider.GetService<IThemeService>().ShouldBeOfType<ThemeService>();
    }

    [Fact]
    public void RegisterIThemeStartupCoordinatorAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<IThemeStartupCoordinator>().ShouldNotBeNull();
        scopedProvider.GetService<IThemeStartupCoordinator>().ShouldBeOfType<ThemeStartupCoordinator>();
    }

    [Fact]
    public void RegisterSettingsViewModelAsScoped()
    {
        IServiceProvider scopedProvider = ExtensionTestsHelper.CreateScopedSystemUnderTest();

        scopedProvider.GetService<SettingsViewModel>().ShouldNotBeNull();
        scopedProvider.GetService<SettingsViewModel>().ShouldBeOfType<SettingsViewModel>();
    }
}