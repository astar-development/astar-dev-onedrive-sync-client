using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.ViewModels;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;
using System;

namespace AStar.Dev.OneDrive.Sync.Client;

/// <summary>
/// Main application class that initializes Avalonia and sets up dependency injection.
/// </summary>
public class App : Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Build configuration
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddUserSecrets<App>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Setup DI container
            var services = new ServiceCollection();
            
            // Register configuration
            services.AddSingleton(configuration);
            
            // Register AppModule services
            services.AddAppModule(configuration);
            
            // Register repositories
            services.AddScoped<IAccountRepository, AccountRepository>();
            
            // Register services
            services.AddScoped<IAuthenticationService, AuthenticationService>();
            services.AddScoped<IAccountCreationService, AccountCreationService>();
            services.AddScoped<IAccountManagementService, AccountManagementService>();
            services.AddScoped<IHashingService, HashingService>();
            
            // Register ViewModels
            services.AddTransient<AddAccountViewModel>();
            services.AddTransient<AccountListViewModel>();
            services.AddTransient<EditAccountViewModel>();
            
            // Register MainWindow
            services.AddTransient<MainWindow>();
            
            _serviceProvider = services.BuildServiceProvider();
            
            // Create and show main window
            desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
