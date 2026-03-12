using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using Avalonia.Controls;

namespace AStar.Dev.OneDrive.Sync.Client.Syncronisation;

/// <summary>
///     View for displaying and managing the OneDrive folder sync tree.
/// </summary>
[ExcludeFromCodeCoverage]
public partial class SyncTreeView : UserControl
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SyncTreeView" /> class.
    /// </summary>
    public SyncTreeView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if(DataContext is SyncTreeViewModel viewModel)
        {
            viewModel.Folders.CollectionChanged += OnRootFoldersChanged;

            foreach(OneDriveFolderNode node in viewModel.Folders)
                AttachNodeExpansionHandler(node, viewModel);
        }
    }

    private void OnRootFoldersChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if(DataContext is not SyncTreeViewModel viewModel)
            return;

        if(e.NewItems != null)
        {
            foreach(OneDriveFolderNode node in e.NewItems)
                AttachNodeExpansionHandler(node, viewModel);
        }
    }

    private static void AttachNodeExpansionHandler(OneDriveFolderNode node, SyncTreeViewModel viewModel)
    {
        node.PropertyChanged += (s, e) =>
        {
            if(e.PropertyName == nameof(OneDriveFolderNode.IsExpanded) &&
               node is { IsExpanded: true, ChildrenLoaded: false })
            {
                _ = viewModel.LoadChildrenCommand.Execute(node).Subscribe();
            }
        };

        node.Children.CollectionChanged += (s, e) =>
        {
            if(e.NewItems != null)
            {
                foreach(OneDriveFolderNode child in e.NewItems)
                    AttachNodeExpansionHandler(child, viewModel);
            }
        };
    }
}
