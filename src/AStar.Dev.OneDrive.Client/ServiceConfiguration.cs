using System.IO.Abstractions;
using System.Net;
using AStar.Dev.OneDrive.Client.Accounts;
using AStar.Dev.OneDrive.Client.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Core.Data;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Client.Infrastructure.Services.Authentication;
using AStar.Dev.OneDrive.Client.MainWindow;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Services.OneDriveServices;
using AStar.Dev.OneDrive.Client.Syncronisation;
using AStar.Dev.OneDrive.Client.SyncronisationConflicts;
using AStar.Dev.Source.Generators.OptionsBindingGeneration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;
using Testably.Abstractions;

namespace AStar.Dev.OneDrive.Client;

/// <summary>
///     Configures dependency injection services for the application.
/// </summary>
public static class ServiceConfiguration
{
    /// <summary>
    ///     Configures and returns the service provider with all application services.
    /// </summary>
    /// <returns>Configured service provider.</returns>
    public static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services = AddDatabaseServices(services);

        _ = services.AddAnnotatedServices(); // as noted in the DebugLogger, this currently isn't working

        IConfigurationRoot configuration = AddApplicationConfiguration(services);

        services = AddAuthentication(services, configuration);

        services = AddApplicationServices(services);

        services = AddViewModels(services);

        _ = services.AddLogging(builder =>
        {
            _ = builder.AddConsole();
            _ = builder.SetMinimumLevel(LogLevel.Information);
        });

        _ = services.AddHostedService<LogCleanupBackgroundService>();

        AddHttpClient(services);

        return services.BuildServiceProvider();
    }

    private static void AddHttpClient(ServiceCollection services)
        => _ = services.AddHttpClient<IGraphApiClient, GraphApiClient>()
                       .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = true, MaxConnectionsPerServer = 10 })
                       .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMinutes(5))
                       .AddPolicyHandler(GetRetryPolicy())
                       .AddPolicyHandler(GetCircuitBreakerPolicy());

    private static ServiceCollection AddAuthentication(ServiceCollection services, IConfigurationRoot configuration)
    {
        using(IServiceScope scope = services.BuildServiceProvider().CreateScope())
        {
            ApplicationSettings appSettings = scope.ServiceProvider.GetRequiredService<IOptions<ApplicationSettings>>().Value;

            EntraIdSettings entraId = scope.ServiceProvider.GetRequiredService<IOptions<EntraIdSettings>>().Value;

            var msalConfigurationSettings = new MsalConfigurationSettings(entraId.ClientId, appSettings.RedirectUri, appSettings.GraphUri, entraId.Scopes ?? [], appSettings.CachePrefix);

            _ = services.AddSingleton(msalConfigurationSettings);
        }

        ;

        var authConfig = AuthConfiguration.LoadFromConfiguration(configuration);

        _ = services.AddSingleton<IAuthService>(provider => AuthService.CreateAsync(authConfig).GetAwaiter().GetResult());

        return services;
    }

    private static ServiceCollection AddViewModels(ServiceCollection services)
    {
        _ = services.AddTransient<AccountManagementViewModel>();
        _ = services.AddTransient<SyncTreeViewModel>();
        _ = services.AddTransient<MainWindowViewModel>();
        _ = services.AddTransient<ConflictResolutionViewModel>();
        _ = services.AddTransient<SyncProgressViewModel>();
        _ = services.AddTransient<UpdateAccountDetailsViewModel>();

        return services;
    }

    private static ServiceCollection AddApplicationServices(ServiceCollection services)
    {
        _ = services.AddSingleton<IFileSystem, RealFileSystem>();
        _ = services.AddSingleton<IFileWatcherService, FileWatcherService>();
        _ = services.AddSingleton<IAutoSyncCoordinator, AutoSyncCoordinator>();
        _ = services.AddSingleton<IAutoSyncSchedulerService, AutoSyncSchedulerService>();
        _ = services.AddScoped<IWindowPreferencesService, WindowPreferencesService>();
        _ = services.AddScoped<IGraphApiClient, GraphApiClient>();
        _ = services.AddScoped<IFolderTreeService, FolderTreeService>();
        _ = services.AddScoped<ISyncSelectionService, SyncSelectionService>();
        _ = services.AddScoped<ILocalFileScanner, LocalFileScanner>();
        _ = services.AddScoped<IRemoteChangeDetector, RemoteChangeDetector>();
        _ = services.AddScoped<IConflictResolver, ConflictResolver>();
        _ = services.AddScoped<ISyncEngine, SyncEngine>();
        _ = services.AddScoped<IDebugLogger, DebugLoggerService>();
        _ = services.AddScoped<IDeltaPageProcessor, DeltaPageProcessor>();
        _ = services.AddScoped<ISyncRepository, EfSyncRepository>();

        return services;
    }

    private static IConfigurationRoot AddApplicationConfiguration(ServiceCollection services)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", false)
            .Build();
        _ = services.AddAutoRegisteredOptions(configuration);
        return configuration;
    }

    private static ServiceCollection AddDatabaseServices(ServiceCollection services)
    {
        _ = services.AddDbContextFactory<SyncDbContext>(options => _ = options.UseSqlite(DatabaseConfiguration.ConnectionString));
        _ = services.AddDbContext<SyncDbContext>(options => options.UseSqlite(DatabaseConfiguration.ConnectionString));
        _ = services.AddScoped<ISyncRepository, EfSyncRepository>();

        _ = services.AddScoped<IAccountRepository, AccountRepository>();
        _ = services.AddScoped<ISyncConfigurationRepository, SyncConfigurationRepository>();
        _ = services.AddScoped<IDriveItemsRepository, DriveItemsRepository>();
        _ = services.AddScoped<ISyncConflictRepository, SyncConflictRepository>();
        _ = services.AddScoped<ISyncSessionLogRepository, SyncSessionLogRepository>();
        _ = services.AddScoped<IFileOperationLogRepository, FileOperationLogRepository>();
        _ = services.AddScoped<IDebugLogRepository, DebugLogRepository>();

        return services;
    }

    /// <summary>
    ///     Ensures the database is created and migrations are applied.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    public static void EnsureDatabaseUpdated(ServiceProvider serviceProvider)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        SyncDbContext context = scope.ServiceProvider.GetRequiredService<SyncDbContext>();

        context.Database.Migrate();
    }

    /// <summary>
    ///     Creates a retry policy with exponential backoff for transient HTTP failures.
    ///     Retries on network failures, 5xx server errors, 429 rate limiting, and IOException.
    /// </summary>
    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
        => Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<IOException>(ex => ex.Message.Contains("forcibly closed") || ex.Message.Contains("transport connection"))
            .OrResult(msg => (int)msg.StatusCode >= 500 || msg.StatusCode == HttpStatusCode.TooManyRequests || msg.StatusCode == HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (outcome, timespan, retryCount, context) =>
                {
                    var error = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown";
                    Console.WriteLine($"[Graph API] Retry {retryCount}/3 after {timespan.TotalSeconds:F1}s. Reason: {error}");
                });

    /// <summary>
    ///     Creates a circuit breaker policy to prevent cascading failures.
    ///     Opens circuit after 5 consecutive failures, stays open for 30 seconds.
    /// </summary>
    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromSeconds(30),
                (outcome, duration) => Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to {outcome.Result?.StatusCode}"),
                () => Console.WriteLine("Circuit breaker reset"));
}
