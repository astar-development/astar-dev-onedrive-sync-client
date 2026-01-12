using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AStarOneDriveClient.Converters;

/// <summary>
/// Converts a boolean value to its negation.
/// </summary>
public sealed class BooleanNegationConverter : IValueConverter
{
    public static readonly BooleanNegationConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return !b;
        return value;
    }
}
