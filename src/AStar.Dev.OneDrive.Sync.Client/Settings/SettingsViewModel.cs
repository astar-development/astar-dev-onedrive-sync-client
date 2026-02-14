using System.Reactive.Linq;
using System.Windows.Input;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using AStar.Dev.Source.Generators.Attributes;
using ReactiveUI;

namespace AStar.Dev.OneDrive.Sync.Client.Settings;

/// <summary>
/// ViewModel for the Settings window, providing theme selection and application.
/// </summary>
[AutoRegisterService]
public class SettingsViewModel : ReactiveObject
{
    private readonly IThemeService _themeService;
    private ThemePreference _selectedTheme;

    public SettingsViewModel(IThemeService themeService)
    {
        _themeService = themeService;
        _selectedTheme = _themeService.CurrentTheme;

        _themeService.ThemeChanged += (_, _) => SelectedTheme = _themeService.CurrentTheme;

        _ = this.WhenAnyValue(x => x.SelectedTheme)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Skip(1)
            .Subscribe(async theme => await ApplyThemeAsync(theme));

        ApplyThemeCommand = ReactiveCommand.CreateFromTask(ExecuteApplyThemeAsync);
    }

    /// <summary>
    /// Gets or sets the currently selected theme.
    /// </summary>
    public ThemePreference SelectedTheme
    {
        get => _selectedTheme;
        set => this.RaiseAndSetIfChanged(ref _selectedTheme, value);
    }

    /// <summary>
    /// Gets or sets the status message displayed after applying theme.
    /// </summary>
    public string? StatusMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Gets all available theme options.
    /// </summary>
    public IEnumerable<ThemePreference> AvailableThemes
        => Enum.GetValues(typeof(ThemePreference)).Cast<ThemePreference>();

    /// <summary>
    /// Command to apply the selected theme.
    /// </summary>
    public ICommand ApplyThemeCommand { get; }

    /// <summary>
    /// Executes the apply theme command.
    /// </summary>
    private async Task ExecuteApplyThemeAsync() => await ApplyThemeAsync(SelectedTheme);

    /// <summary>
    /// Applies the specified theme.
    /// </summary>
    private async Task ApplyThemeAsync(ThemePreference theme)
    {
        try
        {
            await _themeService.ApplyThemeAsync(theme, CancellationToken.None);
            StatusMessage = $"Theme changed to {theme}";
        }
        catch
        {
            // Silently fail, UI remains stable
            StatusMessage = null;
        }
    }
}

