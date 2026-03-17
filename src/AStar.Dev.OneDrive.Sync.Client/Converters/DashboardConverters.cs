using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

/// <summary>Returns red when conflict count > 0, otherwise primary text colour.</summary>
public sealed class ConflictCountToColorConverter : IValueConverter
{
    public static readonly ConflictCountToColorConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int n && n > 0
            ? Color.Parse("#E24B4A")
            : Color.Parse("#1A1917");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Returns true when an integer equals zero — used for empty-state visibility.</summary>
public sealed class IntZeroToBoolConverter : IValueConverter
{
    public static readonly IntZeroToBoolConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int n && n == 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
