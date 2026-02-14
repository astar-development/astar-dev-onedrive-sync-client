using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client.Accounts;

/// <summary>
///     Window for updating account details.
/// </summary>
public sealed partial class UpdateAccountDetailsWindow : Window
{
    public UpdateAccountDetailsWindow()
    {
        InitializeComponent();

        // Retrieve the UpdateAccountDetailsViewModel from DI container
        if(App.Host.Services is not null)
        {
            UpdateAccountDetailsViewModel viewModel = App.Host.Services.GetRequiredService<UpdateAccountDetailsViewModel>();
            DataContext = viewModel;

            // Wire up RequestClose event to close the window
            viewModel.RequestClose += (_, _) => Close();
        }
    }
}
