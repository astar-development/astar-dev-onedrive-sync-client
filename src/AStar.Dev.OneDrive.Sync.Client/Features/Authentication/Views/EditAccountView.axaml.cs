using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.ViewModels;

namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Views;

/// <summary>
/// View for editing account settings including sync directory, concurrency, and debug options.
/// </summary>
public partial class EditAccountView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditAccountView"/> class.
    /// </summary>
    public EditAccountView() => InitializeComponent();

    /// <summary>
    /// Handles the Browse button click to open a folder picker dialog.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private async void OnBrowseDirectoryClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not EditAccountViewModel viewModel)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null)
            return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Home Sync Directory",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            viewModel.HomeSyncDirectory = folders[0].Path.LocalPath;
        }
    }
}
