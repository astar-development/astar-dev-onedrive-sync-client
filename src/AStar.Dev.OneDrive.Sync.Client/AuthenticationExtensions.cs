using AStar.Dev.OneDrive.Sync.Client.ConfigurationSettings;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AStar.Dev.OneDrive.Sync.Client;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        using (IServiceScope scope = services.BuildServiceProvider().CreateScope())
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
}
