using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.Source.Generators.Attributes;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

/// <summary>
/// Service for managing application theme switching at runtime.
/// </summary>
[AutoRegisterService(ServiceLifetime.Singleton, As = typeof(IThemeService))]
public class ThemeService(IWindowPreferencesService windowPreferencesService) : IThemeService
{
    private readonly List<IStyle> _loadedThemeStyles = [];

    /// <inheritdoc />
    public ThemePreference CurrentTheme { get; private set; } = ThemePreference.OriginalAuto;

    /// <inheritdoc />
    public event EventHandler<ThemePreference>? ThemeChanged;

    /// <inheritdoc />
    public async Task ApplyThemeAsync(ThemePreference theme, CancellationToken cancellationToken = default)
    {
        if(Application.Current is not null)
        {
            ApplyThemeVariantAndResources(theme);
        }

        CurrentTheme = theme;

        await PersistThemePreferenceAsync(theme, cancellationToken);

        ThemeChanged?.Invoke(this, theme);
    }

    private void ApplyThemeVariantAndResources(ThemePreference theme)
    {
        if(Application.Current is null)
            return;

        ClearCustomResourceDictionaries();

        switch(theme)
        {
            case ThemePreference.OriginalLight:
                Application.Current.RequestedThemeVariant = ThemeVariant.Light;
                break;

            case ThemePreference.OriginalDark:
                Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
                break;

            case ThemePreference.OriginalAuto:
                Application.Current.RequestedThemeVariant = ThemeVariant.Default;
                break;

            case ThemePreference.Professional:
                Application.Current.RequestedThemeVariant = ThemeVariant.Default;
                LoadThemeResourceDictionary("avares://AStar.Dev.OneDrive.Client/Themes/ProfessionalTheme.axaml");
                break;

            case ThemePreference.Colourful:
                Application.Current.RequestedThemeVariant = ThemeVariant.Default;
                LoadThemeResourceDictionary("avares://AStar.Dev.OneDrive.Client/Themes/ColourfulTheme.axaml");
                break;

            case ThemePreference.Terminal:
                Application.Current.RequestedThemeVariant = ThemeVariant.Default;
                LoadThemeResourceDictionary("avares://AStar.Dev.OneDrive.Client/Themes/TerminalTheme.axaml");
                break;
        }
    }

    private void ClearCustomResourceDictionaries()
    {
        if(Application.Current?.Styles is null)
            return;

        foreach(IStyle? style in _loadedThemeStyles.ToList())
        {
            _ = Application.Current.Styles.Remove(style);
        }

        _loadedThemeStyles.Clear();
    }

    private void LoadThemeResourceDictionary(string resourceUri)
    {
        if(Application.Current is null)
            return;

        try
        {
            var uri = new Uri(resourceUri);
            var styles = (Styles)AvaloniaXamlLoader.Load(uri);
            Application.Current.Styles.Add(styles);
            _loadedThemeStyles.Add(styles);
        }
        catch(Exception ex)
        {
            // Log error but don't fail - fallback to default theme
            System.Diagnostics.Debug.WriteLine($"Failed to load theme styles '{resourceUri}': {ex.Message}");
        }
    }

    private async Task PersistThemePreferenceAsync(ThemePreference theme, CancellationToken cancellationToken)
    {
        WindowPreferences? preferences = await windowPreferencesService.LoadAsync(cancellationToken);

        preferences = preferences is null
            ? new WindowPreferences(Id: 1, X: null, Y: null, Width: 800, Height: 600, IsMaximized: false, Theme: theme)
            : (preferences with { Theme = theme });

        await windowPreferencesService.SaveAsync(preferences, cancellationToken);
    }
}
