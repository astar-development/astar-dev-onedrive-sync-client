using System.Globalization;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Client.Converters;

/// <summary>
/// Converts ThemePreference enum values to user-friendly display names.
/// </summary>
public class ThemePreferenceToDisplayNameConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ThemePreference theme)
            return "Unknown";

        return theme switch
        {
            ThemePreference.OriginalAuto => "Original (Automatic)",
            ThemePreference.OriginalLight => "Original (Light)",
            ThemePreference.OriginalDark => "Original (Dark)",
            ThemePreference.Professional => "Professional",
            ThemePreference.Colourful => "Colourful",
            ThemePreference.Terminal => "Terminal / Hacker",
            _ => "Unknown"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Not supported
        return BindingNotification.UnsetValue;
    }
}
