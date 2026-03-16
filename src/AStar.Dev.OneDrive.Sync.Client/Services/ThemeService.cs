using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using System.Linq;

namespace AStar.Dev.OneDrive.Sync.Client.Services;

/// <summary>
/// Switches between Light, Dark and System themes at runtime by replacing
/// the theme resource dictionary in Application.Current.Resources.
///
/// Expects two ResourceInclude entries already present in App.axaml under
/// the keys "LightThemeInclude" and "DarkThemeInclude" — only one is active
/// at a time.  On System mode it watches
/// <see cref="Application.ActualThemeVariant"/> for OS-level changes.
/// </summary>
public sealed class ThemeService : IThemeService, IDisposable
{
    private static readonly Uri LightUri =
        new("avares://AStar.Dev.OneDrive.Sync.Client/Themes/Light.axaml");
    private static readonly Uri DarkUri =
        new("avares://AStar.Dev.OneDrive.Sync.Client/Themes/Dark.axaml");

    private AppTheme _current = AppTheme.System;
    private IDisposable? _systemWatcher;

    public AppTheme CurrentTheme => _current;
    public event EventHandler<AppTheme>? ThemeChanged;

    public void Apply(AppTheme theme)
    {
        _current = theme;

        _systemWatcher?.Dispose();
        _systemWatcher = null;

        if (theme == AppTheme.System)
        {
            // Apply immediately based on current OS preference, then watch
            ApplyVariant(GetSystemIsDark() ? AppTheme.Dark : AppTheme.Light);
            WatchSystem();
        }
        else
        {
            ApplyVariant(theme);
        }

        ThemeChanged?.Invoke(this, _current);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static bool GetSystemIsDark()
    {
        var app = Application.Current;
        if (app is null) return false;
        return app.ActualThemeVariant == ThemeVariant.Dark;
    }

    private void WatchSystem()
    {
        var app = Application.Current;
        if (app is null) return;

        // ActualThemeVariantChanged fires when the OS dark-mode preference changes
        app.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        _systemWatcher = new Disposable(
            () => app.ActualThemeVariantChanged -= OnActualThemeVariantChanged);
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if (_current != AppTheme.System) return;
        Dispatcher.UIThread.Post(() =>
            ApplyVariant(GetSystemIsDark() ? AppTheme.Dark : AppTheme.Light));
    }

    private static void ApplyVariant(AppTheme resolved)
    {
        var app = Application.Current;
        if (app is null) return;

        var targetUri = resolved == AppTheme.Dark ? DarkUri : LightUri;
        var resources = app.Resources.MergedDictionaries;

        var existing = resources
            .OfType<ResourceInclude>()
            .FirstOrDefault(r => r.Source == LightUri || r.Source == DarkUri);

        if (existing is not null)
            resources.Remove(existing);

        resources.Add(new ResourceInclude(targetUri) { Source = targetUri });
    }

    public void Dispose() => _systemWatcher?.Dispose();

    // Minimal disposable helper to avoid a System.Reactive dependency
    private sealed class Disposable(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
