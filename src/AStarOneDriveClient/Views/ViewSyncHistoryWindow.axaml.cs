using Avalonia.Controls;
using AStarOneDriveClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AStarOneDriveClient.Views;

/// <summary>
/// Window for viewing sync history.
/// </summary>
public sealed partial class ViewSyncHistoryWindow : Window
{
    public ViewSyncHistoryWindow()
    {
        InitializeComponent();

        // Retrieve the ViewSyncHistoryViewModel from DI container
        if (App.Services is not null)
        {
            DataContext = App.Services.GetRequiredService<ViewSyncHistoryViewModel>();
        }
    }
}
