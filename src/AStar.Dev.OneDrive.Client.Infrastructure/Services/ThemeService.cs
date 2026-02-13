using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.Source.Generators.Attributes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

/// <summary>
/// Service for managing application theme switching at runtime.
/// </summary>
[AutoRegisterService(ServiceLifetime.Singleton, As = typeof(IThemeService))]
public class ThemeService(IWindowPreferencesService windowPreferencesService) : IThemeService
{
    private ThemePreference _currentTheme = ThemePreference.OriginalAuto;
    private readonly List<ResourceDictionary> _loadedThemeDictionaries = new();

    /// <inheritdoc />
    public ThemePreference CurrentTheme => _currentTheme;

    /// <inheritdoc />
    public event EventHandler<ThemePreference>? ThemeChanged;

    /// <inheritdoc />
    public async Task ApplyThemeAsync(ThemePreference theme, CancellationToken cancellationToken = default)
    {
        // Update runtime theme if Application.Current is available
        if(Application.Current is not null)
        {
            ApplyThemeVariantAndResources(theme);
        }

        // Update current theme
        _currentTheme = theme;

        // Persist to window preferences
        await PersistThemePreferenceAsync(theme, cancellationToken);

        // Raise event
        ThemeChanged?.Invoke(this, theme);
    }

    private void ApplyThemeVariantAndResources(ThemePreference theme)
    {
        if(Application.Current is null)
            return;

        // Clear custom resource dictionaries (preserve base FluentTheme)
        ClearCustomResourceDictionaries();

        // Apply theme based on preference
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
        if(Application.Current?.Resources.MergedDictionaries is null)
            return;

        foreach(var dictionary in _loadedThemeDictionaries.ToList())
        {
            _ = Application.Current.Resources.MergedDictionaries.Remove(dictionary);
        }

        _loadedThemeDictionaries.Clear();
    }

    private void LoadThemeResourceDictionary(string resourceUri)
    {
        if(Application.Current is null)
            return;

        try
        {
            var uri = new Uri(resourceUri);
            var resourceDictionary = (ResourceDictionary)AvaloniaXamlLoader.Load(uri);
            Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            _loadedThemeDictionaries.Add(resourceDictionary);
        }
        catch(Exception ex)
        {
            // Log error but don't fail - fallback to default theme
            System.Diagnostics.Debug.WriteLine($"Failed to load theme resource dictionary '{resourceUri}': {ex.Message}");
        }
    }

    private async Task PersistThemePreferenceAsync(ThemePreference theme, CancellationToken cancellationToken)
    {
        // Load existing preferences or create new ones
        var preferences = await windowPreferencesService.LoadAsync(cancellationToken);

        if(preferences is null)
        {
            // Create default preferences with new theme
            preferences = new WindowPreferences(
                Id: 1,
                X: null,
                Y: null,
                Width: 800,
                Height: 600,
                IsMaximized: false,
                Theme: theme);
        }
        else
        {
            // Update theme in existing preferences
            preferences = preferences with { Theme = theme };
        }

        await windowPreferencesService.SaveAsync(preferences, cancellationToken);
    }
}
