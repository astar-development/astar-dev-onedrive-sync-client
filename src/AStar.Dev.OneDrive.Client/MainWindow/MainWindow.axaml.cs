using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace AStar.Dev.OneDrive.Client.MainWindow;

/// <summary>
///     Main application window.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly IWindowPreferencesService _preferencesService;
    private readonly DispatcherTimer _savePreferencesTimer;

    public MainWindow()
    {
        InitializeComponent();

        DataContext = App.Host.Services.GetRequiredService<MainWindowViewModel>();
        _preferencesService = App.Host.Services.GetRequiredService<IWindowPreferencesService>();
        _ = LoadWindowPreferencesAsync();

        PositionChanged += OnPositionChanged;
        PropertyChanged += OnWindowPropertyChanged;

        _savePreferencesTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _savePreferencesTimer.Tick += async (s, e) =>
        {
            _savePreferencesTimer.Stop();
            await SaveWindowPreferencesAsync();
        };
    }

    private async Task LoadWindowPreferencesAsync()
    {
        if(_preferencesService is null)
            return;

        try
        {
            WindowPreferences? preferences = await _preferencesService.LoadAsync();
            if(preferences is not null)
            {
                if(preferences.IsMaximized)
                {
                    WindowState = WindowState.Maximized;
                }
                else if(preferences is { X: not null, Y: not null })
                {
                    Position = new PixelPoint((int)preferences.X.Value, (int)preferences.Y.Value);
                    Width = preferences.Width;
                    Height = preferences.Height;
                }
            }
        }
        catch
        {
            // Ignore errors loading preferences - use defaults
        }
    }

    private void OnPositionChanged(object? sender, PixelPointEventArgs e)
    {
        _savePreferencesTimer?.Stop();
        _savePreferencesTimer?.Start();
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if(e.Property == WindowStateProperty || e.Property == WidthProperty || e.Property == HeightProperty)
        {
            _savePreferencesTimer?.Stop();
            _savePreferencesTimer?.Start();
        }
    }

    private async Task SaveWindowPreferencesAsync()
    {
        if(_preferencesService is null)
            return;

        try
        {
            var preferences = new WindowPreferences(
                1,
                WindowState == WindowState.Normal ? Position.X : null,
                WindowState == WindowState.Normal ? Position.Y : null,
                Width,
                Height,
                WindowState == WindowState.Maximized
            );

            await _preferencesService.SaveAsync(preferences);
        }
        catch
        {
            // Ignore errors saving preferences
        }
    }
}
