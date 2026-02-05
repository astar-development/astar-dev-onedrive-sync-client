using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Views;

public partial class AddAccountView : UserControl
{
    public AddAccountView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
