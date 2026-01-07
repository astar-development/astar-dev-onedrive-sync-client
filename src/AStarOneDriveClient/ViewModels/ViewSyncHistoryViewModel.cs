using ReactiveUI;

namespace AStarOneDriveClient.ViewModels;

/// <summary>
/// ViewModel for the View Sync History window.
/// </summary>
public sealed class ViewSyncHistoryViewModel : ReactiveObject
{
    /// <summary>
    /// Gets the placeholder message.
    /// </summary>
    public static string PlaceholderMessage => "Sync history viewing coming soon";
}
