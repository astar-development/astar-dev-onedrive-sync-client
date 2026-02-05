using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Sync.Client;

/// <summary>
/// Main application window that integrates all authentication and account management UI components.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    public MainWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();

        UserControl? accountListView = this.FindControl<UserControl>("AccountListView");
        UserControl? addAccountView = this.FindControl<UserControl>("AddAccountView");
        UserControl? editAccountView = this.FindControl<UserControl>("EditAccountView");
        
        if (accountListView != null)
        {
            AccountListViewModel accountListViewModel = serviceProvider.GetRequiredService<AccountListViewModel>();
            accountListView.DataContext = accountListViewModel;

            accountListViewModel.LoadAccountsCommand.Execute(null);
            
            accountListViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AccountListViewModel.SelectedAccount) && 
                    editAccountView != null &&
                    accountListViewModel.SelectedAccount != null)
                {
                    EditAccountViewModel editAccountViewModel = serviceProvider.GetRequiredService<EditAccountViewModel>();
                    editAccountViewModel.LoadAccount(accountListViewModel.SelectedAccount);
                    editAccountView.DataContext = editAccountViewModel;
                }
            };
        }
        
        if (addAccountView != null)
        {
            AddAccountViewModel addAccountViewModel = serviceProvider.GetRequiredService<AddAccountViewModel>();
            addAccountView.DataContext = addAccountViewModel;
            
            addAccountViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AddAccountViewModel.CreatedAccount) &&
                    addAccountViewModel.CreatedAccount != null &&
                    accountListView?.DataContext is AccountListViewModel accountListVm)
                {
                    accountListVm.LoadAccountsCommand.Execute(null);
                }
            };
        }
        
        if (editAccountView != null)
        {
            // EditAccountViewModel will be set dynamically when an account is selected
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
