using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;
using Avalonia.Threading;

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
    private IDisposable? _systemWatcher;

    public AppTheme CurrentTheme { get; private set; } = AppTheme.System;
    public event EventHandler<AppTheme>? ThemeChanged;

    public void Apply(AppTheme theme)
    {
        CurrentTheme = theme;

        _systemWatcher?.Dispose();
        _systemWatcher = null;

        if(theme == AppTheme.System)
        {
            ApplyVariant(GetSystemIsDark() ? AppTheme.Dark : AppTheme.Light);
            WatchSystem();
        }
        else
        {
            ApplyVariant(theme);
        }

        ThemeChanged?.Invoke(this, CurrentTheme);
    }

    private static bool GetSystemIsDark()
    {
        Application? app = Application.Current;
        return app is not null && app.ActualThemeVariant == ThemeVariant.Dark;
    }

    private void WatchSystem()
    {
        Application? app = Application.Current;
        if(app is null)
            return;

        app.ActualThemeVariantChanged += OnActualThemeVariantChanged;
        _systemWatcher = new Disposable(
            () => app.ActualThemeVariantChanged -= OnActualThemeVariantChanged);
    }

    private void OnActualThemeVariantChanged(object? sender, EventArgs e)
    {
        if(CurrentTheme != AppTheme.System)
            return;
        Dispatcher.UIThread.Post(() =>
            ApplyVariant(GetSystemIsDark() ? AppTheme.Dark : AppTheme.Light));
    }

    private static void ApplyVariant(AppTheme resolved)
    {
        Application? app = Application.Current;
        if(app is null)
            return;

        Uri targetUri = resolved == AppTheme.Dark ? DarkUri : LightUri;
        IList<IResourceProvider> merged = app.Resources.MergedDictionaries;

        ResourceInclude? existing = merged
            .OfType<ResourceInclude>()
            .FirstOrDefault(r => r.Source == LightUri || r.Source == DarkUri);

        if(existing is not null)
            _ = merged.Remove(existing);

        merged.Add(new ResourceInclude(targetUri) { Source = targetUri });
    }

    public void Dispose() => _systemWatcher?.Dispose();

    private sealed class Disposable(Action action) : IDisposable
    {
        public void Dispose() => action();
    }
}
