using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

/// <summary>Converts bool IsExpanded to "Collapse" / "Resolve" label.</summary>
public sealed class BoolToCollapseExpandConverter : IValueConverter
{
    public static readonly BoolToCollapseExpandConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Collapse" : "Resolve";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
