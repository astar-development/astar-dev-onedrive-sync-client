using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

/// <summary>
/// Converts ThemePreference enum values to user-friendly display names.
/// </summary>
public class ThemePreferenceToDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not ThemePreference theme
            ? "Unknown"
            : theme switch
            {
                ThemePreference.OriginalAuto => "Original (Automatic)",
                ThemePreference.OriginalLight => "Original (Light)",
                ThemePreference.OriginalDark => "Original (Dark)",
                ThemePreference.Professional => "Professional",
                ThemePreference.Colourful => "Colourful",
                ThemePreference.Terminal => "Terminal / Hacker",
                _ => "Unknown"
            };

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        // Not supported
        => BindingNotification.UnsetValue;
}
