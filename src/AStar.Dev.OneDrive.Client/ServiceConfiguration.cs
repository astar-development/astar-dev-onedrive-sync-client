using System.IO.Abstractions;
using AStar.Dev.OneDrive.Client.Accounts;
using AStar.Dev.OneDrive.Client.Core.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Client.Services;
using AStar.Dev.OneDrive.Client.Services.OneDriveServices;
using AStar.Dev.OneDrive.Client.Services.Sync;
using AStar.Dev.OneDrive.Client.Syncronisation;
using AStar.Dev.OneDrive.Client.SyncronisationConflicts;
using AStar.Dev.OneDrive.Client.MainWindow;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;
using Testably.Abstractions;
using AStar.Dev.OneDrive.Client.Infrastructure.Services.Authentication;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Core.DTOs;
using AStar.Dev.OneDrive.Client.ConfigurationSettings;
using AStar.Dev.OneDrive.Client.Services.ConfigurationSettings;
using Microsoft.Extensions.Options;
using AStar.Dev.Source.Generators.OptionsBindingGeneration;

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

        // Database
        _ = services.AddDbContextFactory<SyncDbContext>(options => _ = options.UseSqlite(DatabaseConfiguration.ConnectionString));
        _ = services.AddDbContext<SyncDbContext>(options => options.UseSqlite(DatabaseConfiguration.ConnectionString));
        _ = services.AddScoped<ISyncRepository>(sp =>
        {
            IDbContextFactory<SyncDbContext> factory = sp.GetRequiredService<IDbContextFactory<SyncDbContext>>();
            ILogger<EfSyncRepository> logger = sp.GetRequiredService<ILogger<EfSyncRepository>>();
            return new EfSyncRepository(factory, logger);
        });

        _ = services.AddAnnotatedServices(); // as noted in the DebugLogger, this currently isn't working
        // Repositories
        _ = services.AddScoped<IAccountRepository, AccountRepository>();
        _ = services.AddScoped<ISyncConfigurationRepository, SyncConfigurationRepository>();
        _ = services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();
        _ = services.AddScoped<ISyncConflictRepository, SyncConflictRepository>();
        _ = services.AddScoped<ISyncSessionLogRepository, SyncSessionLogRepository>();
        _ = services.AddScoped<IFileOperationLogRepository, FileOperationLogRepository>();
        _ = services.AddScoped<IDebugLogRepository, DebugLogRepository>();

        // Load authentication configuration
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", false)
            .Build();
        _ = services.AddAutoRegisteredOptions(configuration);

        var connectionString = string.Empty;
        var localRoot = string.Empty;
        var msalClientId = string.Empty;
        using(IServiceScope scope = services.BuildServiceProvider().CreateScope())
        {
            ApplicationSettings appSettings = scope.ServiceProvider.GetRequiredService<IOptions<ApplicationSettings>>().Value;
            
            EntraIdSettings entraId = scope.ServiceProvider.GetRequiredService<IOptions<EntraIdSettings>>().Value;

            connectionString = $"Data Source={appSettings.FullDatabasePath}";
            localRoot = appSettings.FullUserSyncPath;
            msalClientId = entraId.ClientId;

            var msalConfigurationSettings = new MsalConfigurationSettings(
            msalClientId,
            appSettings.RedirectUri,
            appSettings.GraphUri,
            entraId.Scopes ?? [],
            appSettings.CachePrefix);

            _ = services.AddSingleton(msalConfigurationSettings);
        };

        var authConfig = AuthConfiguration.LoadFromConfiguration(configuration);

        // Authentication - registered as singleton with factory
        _ = services.AddSingleton<IAuthService>(provider =>
            // AuthService.CreateAsync must be called synchronously during startup
            // This is acceptable as it's a one-time initialization cost
            AuthService.CreateAsync(authConfig).GetAwaiter().GetResult());

        // Services
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
        _ = services.AddScoped<IDebugLogger, DebugLogger>();
        _ = services.AddScoped<IDeltaPageProcessor, DeltaPageProcessor>();
        _ = services.AddScoped<ISyncRepository, EfSyncRepository>();

        // ViewModels
        _ = services.AddTransient<AccountManagementViewModel>();
        _ = services.AddTransient<SyncTreeViewModel>();
        _ = services.AddTransient<MainWindowViewModel>();
        _ = services.AddTransient<ConflictResolutionViewModel>();
        _ = services.AddTransient<SyncProgressViewModel>();
        _ = services.AddTransient<UpdateAccountDetailsViewModel>();

        // Logging
        _ = services.AddLogging(builder =>
        {
            _ = builder.AddConsole();
            _ = builder.SetMinimumLevel(LogLevel.Information);
        });

        // Background Services
        _ = services.AddHostedService<LogCleanupBackgroundService>();

        _ = services.AddHttpClient<IGraphApiClient, GraphApiClient>()
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    AllowAutoRedirect = true,
                    MaxConnectionsPerServer = 10
                })
                .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMinutes(5))
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());

        return services.BuildServiceProvider();
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
    /// Creates a retry policy with exponential backoff for transient HTTP failures.
    /// Retries on network failures, 5xx server errors, 429 rate limiting, and IOException.
    /// </summary>
    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy()
        => Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .Or<IOException>(ex => ex.Message.Contains("forcibly closed") || ex.Message.Contains("transport connection"))
            .OrResult(msg => (int)msg.StatusCode >= 500 || msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests || msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    var error = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown";
                    Console.WriteLine($"[Graph API] Retry {retryCount}/3 after {timespan.TotalSeconds:F1}s. Reason: {error}");
                });

    /// <summary>
    /// Creates a circuit breaker policy to prevent cascading failures.
    /// Opens circuit after 5 consecutive failures, stays open for 30 seconds.
    /// </summary>
    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        => HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                    Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to {outcome.Result?.StatusCode}"),
                onReset: () =>
                    Console.WriteLine("Circuit breaker reset"));
}
