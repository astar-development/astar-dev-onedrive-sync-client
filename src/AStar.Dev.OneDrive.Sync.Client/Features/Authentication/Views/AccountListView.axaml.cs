using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Views;

public partial class AccountListView : UserControl
{
    public AccountListView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
