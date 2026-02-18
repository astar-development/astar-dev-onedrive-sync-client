using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AStar.Dev.OneDrive.Sync.Client.Settings;

[ExcludeFromCodeCoverage]
public partial class SettingsWindow : Window
{
    public SettingsWindow() => InitializeComponent();

    private void CloseButton_Click(object? sender, RoutedEventArgs e) => Close();
}
