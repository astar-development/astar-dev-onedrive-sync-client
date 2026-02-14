namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
/// Coordinates theme initialization on application startup.
/// </summary>
public interface IThemeStartupCoordinator
{
    /// <summary>
    /// Initializes the application theme on startup.
    /// Loads saved preferences and applies the theme, or applies default if none exist or loading fails.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    Task InitializeThemeOnStartupAsync(CancellationToken cancellationToken = default);
}
