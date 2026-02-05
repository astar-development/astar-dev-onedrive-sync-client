using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

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
        
        // Wire up ViewModels
        var accountListView = this.FindControl<UserControl>("AccountListView");
        var addAccountView = this.FindControl<UserControl>("AddAccountView");
        var editAccountView = this.FindControl<UserControl>("EditAccountView");
        
        if (accountListView != null)
        {
            var accountListViewModel = serviceProvider.GetRequiredService<AccountListViewModel>();
            accountListView.DataContext = accountListViewModel;

            accountListViewModel.LoadAccountsCommand.Execute(null);
            
            accountListViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AccountListViewModel.SelectedAccount) && 
                    editAccountView != null &&
                    accountListViewModel.SelectedAccount != null)
                {
                    var editAccountViewModel = serviceProvider.GetRequiredService<EditAccountViewModel>();
                    editAccountViewModel.LoadAccount(accountListViewModel.SelectedAccount);
                    editAccountView.DataContext = editAccountViewModel;
                }
            };
        }
        
        if (addAccountView != null)
        {
            var addAccountViewModel = serviceProvider.GetRequiredService<AddAccountViewModel>();
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
