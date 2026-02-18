using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.MainWindow;
using AStar.Dev.OneDrive.Sync.Client.Syncronisation;
using AStar.Dev.OneDrive.Sync.Client.SyncronisationConflicts;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client;

public static class ViewModelExtensions
{
    public static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        _ = services.AddTransient<AccountManagementViewModel>();
        _ = services.AddTransient<SyncTreeViewModel>();
        _ = services.AddTransient<MainWindowViewModel>();
        _ = services.AddTransient<ConflictResolutionViewModel>();
        _ = services.AddTransient<SyncProgressViewModel>();
        _ = services.AddTransient<UpdateAccountDetailsViewModel>();

        return services;
    }
}
