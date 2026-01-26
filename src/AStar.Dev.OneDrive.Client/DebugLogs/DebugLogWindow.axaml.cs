using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client.DebugLogs;

/// <summary>
///     Window for viewing debug logs.
/// </summary>
public partial class DebugLogWindow : Window
{
    public DebugLogWindow()
    {
        InitializeComponent();

        if(App.Services is not null)
        {
            IAccountRepository accountRepository = App.Services.GetRequiredService<IAccountRepository>();
            IDebugLogRepository debugLogRepository = App.Services.GetRequiredService<IDebugLogRepository>();
            IDebugLogger debugLogger = App.Services.GetRequiredService<IDebugLogger>();
            DataContext = new DebugLogViewModel(accountRepository, debugLogRepository, debugLogger);
        }
    }
}
