using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.ViewModels;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;

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
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddUserSecrets<App>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            var services = new ServiceCollection();

            _ = services.AddSingleton(configuration);

            _ = services.AddAppModule(configuration);

            _ = services.AddScoped<IAccountRepository, AccountRepository>();

            _ = services.AddScoped<IAuthenticationService, AuthenticationService>();
            _ = services.AddScoped<IAccountCreationService, AccountCreationService>();
            _ = services.AddScoped<IAccountManagementService, AccountManagementService>();
            _ = services.AddScoped<IHashingService, HashingService>();

            _ = services.AddTransient<AddAccountViewModel>();
            _ = services.AddTransient<AccountListViewModel>();
            _ = services.AddTransient<EditAccountViewModel>();

            _ = services.AddTransient<MainWindow>();
            
            _serviceProvider = services.BuildServiceProvider();
            
            desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
