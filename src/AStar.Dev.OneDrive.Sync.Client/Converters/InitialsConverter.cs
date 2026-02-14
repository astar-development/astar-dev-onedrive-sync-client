using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

public class InitialsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is not string name || string.IsNullOrWhiteSpace(name))
            return "?";

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return parts.Length == 0 ? "?" : parts.Length == 1 ? parts[0][0].ToString().ToUpper() : $"{parts[0][0]}{parts[^1][0]}".ToUpper();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}
