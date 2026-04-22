using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

[ExcludeFromCodeCoverage]
/// <summary>Converts bool IsIncluded to tooltip text.</summary>
public sealed class BoolToExcludeIncludeTooltipConverter : IValueConverter, IBoolToExcludeIncludeTooltipConverter
{
    public static readonly BoolToExcludeIncludeTooltipConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? "Exclude this folder from sync" : "Include this folder in sync";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
