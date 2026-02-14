using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

/// <summary>
/// Service for managing application theme switching at runtime.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets the currently applied theme.
    /// </summary>
    ThemePreference CurrentTheme { get; }

    /// <summary>
    /// Raised when the theme is changed.
    /// </summary>
    event EventHandler<ThemePreference>? ThemeChanged;

    /// <summary>
    /// Applies the specified theme to the application at runtime.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ApplyThemeAsync(ThemePreference theme, CancellationToken cancellationToken = default);
}
