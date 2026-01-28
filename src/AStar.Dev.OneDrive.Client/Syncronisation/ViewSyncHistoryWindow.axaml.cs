using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client.Syncronisation;

/// <summary>
///     Window for viewing sync history.
/// </summary>
public sealed partial class ViewSyncHistoryWindow : Window
{
    public ViewSyncHistoryWindow()
    {
        InitializeComponent();

        // Retrieve dependencies from DI container and create ViewModel
        if(App.Services is not null)
        {
            IAccountRepository accountRepo = App.Services.GetRequiredService<IAccountRepository>();
            IFileOperationLogRepository fileOpLogRepo = App.Services.GetRequiredService<IFileOperationLogRepository>();
            IDebugLogger debugLogger = App.Services.GetRequiredService<IDebugLogger>();
            DataContext = new ViewSyncHistoryViewModel(accountRepo, fileOpLogRepo, debugLogger);
        }
    }
}
