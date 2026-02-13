using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using ReactiveUI;
using System.Windows.Input;

namespace AStar.Dev.OneDrive.Client.Settings;

/// <summary>
/// ViewModel for the Settings window, providing theme selection and application.
/// </summary>
public class SettingsViewModel : ReactiveObject
{
    private readonly IThemeService _themeService;
    private ThemePreference _selectedTheme;
    private string? _statusMessage;

    public SettingsViewModel(IThemeService themeService)
    {
        _themeService = themeService;
        _selectedTheme = _themeService.CurrentTheme;

        // Subscribe to external theme changes
        _themeService.ThemeChanged += (_, _) =>
        {
            SelectedTheme = _themeService.CurrentTheme;
        };

        // Create ApplyThemeCommand
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
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }

    /// <summary>
    /// Gets all available theme options.
    /// </summary>
    public IEnumerable<ThemePreference> AvailableThemes =>
        Enum.GetValues(typeof(ThemePreference)).Cast<ThemePreference>();

    /// <summary>
    /// Command to apply the selected theme.
    /// </summary>
    public ICommand ApplyThemeCommand { get; }

    /// <summary>
    /// Executes the apply theme command.
    /// </summary>
    private async Task ExecuteApplyThemeAsync()
    {
        try
        {
            await _themeService.ApplyThemeAsync(SelectedTheme, CancellationToken.None);
            StatusMessage = "Theme applied successfully";
        }
        catch
        {
            // Silently fail, UI remains stable
            StatusMessage = null;
        }
    }
}

