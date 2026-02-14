using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

public class BoolToStatusTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool isAuthenticated ? isAuthenticated ? "✓ Connected" : "✗ Disconnected" : "Unknown";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
