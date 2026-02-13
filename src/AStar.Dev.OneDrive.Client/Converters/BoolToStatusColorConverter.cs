using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace AStar.Dev.OneDrive.Client.Converters;

public class BoolToStatusColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is bool isAuthenticated
            ? isAuthenticated
                ? new SolidColorBrush(Color.Parse("#4ECDC4"))
                : new SolidColorBrush(Color.Parse("#FF6B6B"))
            : new SolidColorBrush(Color.Parse("#CCCCCC"));

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
