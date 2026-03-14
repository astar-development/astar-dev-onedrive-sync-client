using System.Diagnostics.CodeAnalysis;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.DebugLogs;

/// <summary>
///     Window for viewing debug logs.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class DebugLogWindow : Window
{
    public DebugLogWindow()
    {
        InitializeComponent();

        if(Start.App.Host.Services is not null)
        {
            IAccountRepository accountRepository = Start.App.Host.Services.GetRequiredService<IAccountRepository>();
            IDebugLogRepository debugLogRepository = Start.App.Host.Services.GetRequiredService<IDebugLogRepository>();
            IDebugLogger debugLogger = Start.App.Host.Services.GetRequiredService<IDebugLogger>();
            DataContext = new DebugLogViewModel(accountRepository, debugLogRepository, debugLogger);
        }
    }
}
