using System.Globalization;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

public interface IBoolToExcludeIncludeTooltipConverter
{
    object Convert(object? value, Type targetType, object? parameter, CultureInfo culture);
    object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture);
}
