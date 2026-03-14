
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Start;

public class AuthenticationExtensionsShould
{    
    [Fact]
    public void RegisterIAuthServiceAsSingleton()
    {
        ServiceProvider provider = ExtensionTestsHelper.CreateSystemUnderTest();

        IAuthService? authService1 = provider.GetService<IAuthService>();

        authService1.ShouldNotBeNull();
        authService1.ShouldBeOfType<AuthService>();

        IAuthService? authService2 = provider.GetService<IAuthService>();
        authService2?.Equals(authService1).ShouldBeTrue();
    }

    [Fact]
    public void RegisterMsalConfigurationSettingsAsSingleton()
    {
        ServiceProvider provider = ExtensionTestsHelper.CreateSystemUnderTest();

        MsalConfigurationSettings? msal1 = provider.GetService<MsalConfigurationSettings>();

        msal1.ShouldNotBeNull();
        msal1.ShouldBeOfType<MsalConfigurationSettings>();
        MsalConfigurationSettings? msal2 = provider.GetService<MsalConfigurationSettings>();
        msal2?.Equals(msal1).ShouldBeTrue();
    }
}