
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.Source.Generators.Attributes;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;
/// <summary>
/// Coordinates theme initialization on application startup.
/// Loads saved theme preferences and applies the appropriate theme at startup.
/// </summary>
[AutoRegisterService(ServiceLifetime.Singleton, As = typeof(IThemeStartupCoordinator))]
public class ThemeStartupCoordinator(
    IThemeService themeService,
    IWindowPreferencesService windowPreferencesService) : IThemeStartupCoordinator
{
    /// <summary>
    /// Initializes the application theme on startup.
    /// Loads saved preferences and applies the theme, or applies default if none exist or loading fails.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop the operation.</param>
    /// <remarks>
    /// This method handles errors gracefully and will not throw exceptions.
    /// If theme application fails, the application continues with the default theme.
    /// </remarks>
    public async Task InitializeThemeOnStartupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            WindowPreferences? preferences = await windowPreferencesService.LoadAsync(cancellationToken);
            ThemePreference themeToApply = preferences?.Theme ?? ThemePreference.OriginalAuto;
            await themeService.ApplyThemeAsync(themeToApply, cancellationToken);
        }
        catch(Exception)
        {
            // Silently fail - application continues with default theme
            // Error logging could be added here if logger is injected
        }
    }
}

