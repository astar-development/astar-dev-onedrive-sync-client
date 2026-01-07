using Avalonia.Controls;
using AStarOneDriveClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AStarOneDriveClient.Views;

/// <summary>
/// Window for updating account details.
/// </summary>
public sealed partial class UpdateAccountDetailsWindow : Window
{
    public UpdateAccountDetailsWindow()
    {
        InitializeComponent();

        // Retrieve the UpdateAccountDetailsViewModel from DI container
        if (App.Services is not null)
        {
            DataContext = App.Services.GetRequiredService<UpdateAccountDetailsViewModel>();
        }
    }
}
