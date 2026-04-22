using System.Diagnostics.CodeAnalysis;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Sync.Client;

[ExcludeFromCodeCoverage]
public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();

    public async Task InitialiseAsync(MainWindowViewModel vm)
    {
        DataContext = vm;
        await vm.InitialiseAsync();
    }
}
