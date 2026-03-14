using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Home;
using AStar.Dev.OneDrive.Sync.Client.Start;
using AStar.Dev.OneDrive.Sync.Client.Syncronisation;
using AStar.Dev.OneDrive.Sync.Client.SyncronisationConflicts;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Start;

public class ViewModelExtensionsShould
{
    [Fact]
    public void RegisterAccountManagementViewModelAsTransient()
    {
        ServiceProvider provider = ExtensionTestsHelper.CreateSystemUnderTest();
        
        AccountManagementViewModel? viewModel1 = provider.GetService<AccountManagementViewModel>();
        AccountManagementViewModel? viewModel2 = provider.GetService<AccountManagementViewModel>();

        viewModel1.ShouldNotBeNull();
        viewModel2.ShouldNotBeNull();
        viewModel1.ShouldNotBeSameAs(viewModel2);
    }

    [Fact]
    public void RegisterSyncTreeViewModelAsTransient()
    {
        ServiceProvider provider = ExtensionTestsHelper.CreateSystemUnderTest();
        
        SyncTreeViewModel? viewModel1 = provider.GetService<SyncTreeViewModel>();
        SyncTreeViewModel? viewModel2 = provider.GetService<SyncTreeViewModel>();

        viewModel1.ShouldNotBeNull();
        viewModel2.ShouldNotBeNull();
        viewModel1.ShouldNotBeSameAs(viewModel2);
    }

    [Fact]
    public void RegisterMainWindowViewModelAsTransient()
    {
        ServiceProvider provider = ExtensionTestsHelper.CreateSystemUnderTest();
        
        MainWindowViewModel? viewModel1 = provider.GetService<MainWindowViewModel>();
        MainWindowViewModel? viewModel2 = provider.GetService<MainWindowViewModel>();

        viewModel1.ShouldNotBeNull();
        viewModel2.ShouldNotBeNull();
        viewModel1.ShouldNotBeSameAs(viewModel2);
    }

    [Fact(Skip = "ConflictResolutionViewModel requires the string accountId to be injected but, we cannot provide it. We need to refactor the service to avoid this dependency.")]
    public void RegisterConflictResolutionViewModelAsTransient()
    {
        ServiceProvider provider = ExtensionTestsHelper.CreateSystemUnderTest();
        
        ConflictResolutionViewModel? viewModel1 = provider.GetService<ConflictResolutionViewModel>();
        ConflictResolutionViewModel? viewModel2 = provider.GetService<ConflictResolutionViewModel>();

        viewModel1.ShouldNotBeNull();
        viewModel2.ShouldNotBeNull();
        viewModel1.ShouldNotBeSameAs(viewModel2);
    }

    [Fact(Skip = "ConflictResolutionViewModel requires the string accountId to be injected but, we cannot provide it. We need to refactor the service to avoid this dependency.")]
    public void RegisterSyncProgressViewModelAsTransient()
    {
        ServiceProvider provider = ExtensionTestsHelper.CreateSystemUnderTest();
        
        SyncProgressViewModel? viewModel1 = provider.GetService<SyncProgressViewModel>();
        SyncProgressViewModel? viewModel2 = provider.GetService<SyncProgressViewModel>();

        viewModel1.ShouldNotBeNull();
        viewModel2.ShouldNotBeNull();
        viewModel1.ShouldNotBeSameAs(viewModel2);
    }

    [Fact]
    public void RegisterUpdateAccountDetailsViewModelAsTransient()
    {
        ServiceProvider provider = ExtensionTestsHelper.CreateSystemUnderTest();

        UpdateAccountDetailsViewModel? viewModel1 = provider.GetService<UpdateAccountDetailsViewModel>();
        UpdateAccountDetailsViewModel? viewModel2 = provider.GetService<UpdateAccountDetailsViewModel>();

        viewModel1.ShouldNotBeNull();
        viewModel2.ShouldNotBeNull();
        viewModel1.ShouldNotBeSameAs(viewModel2);
    }

    [Fact]
    public void ReturnServiceCollectionForMethodChaining()
    {
        IServiceCollection services = new ServiceCollection();
        IServiceCollection result = services.AddViewModels();

        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void BuildServiceProviderSuccessfully()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddViewModels();

        ServiceProvider? provider = null;
        Shouldly.Should.NotThrow(() => provider = services.BuildServiceProvider());

        provider.ShouldNotBeNull();
    }
}