using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;
using System;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

public sealed class SyncStateToForegroundConverter : IValueConverter
{
    public static readonly SyncStateToForegroundConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not SyncState state)
            return Brushes.Transparent;

        return state switch
        {
            SyncState.Syncing  => new SolidColorBrush(Color.Parse("#185FA5")),
            SyncState.Pending  => new SolidColorBrush(Color.Parse("#BA7517")),
            SyncState.Conflict => new SolidColorBrush(Color.Parse("#E24B4A")),
            SyncState.Error    => new SolidColorBrush(Color.Parse("#E24B4A")),
            _                  => new SolidColorBrush(Color.Parse("#1D9E75")),
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}