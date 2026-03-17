using Avalonia.Data.Converters;
using Avalonia.Media;
using AStar.Dev.OneDrive.Sync.Client.Models;
using System.Globalization;

namespace AStar.Dev.OneDrive.Sync.Client.Converters;

/// <summary>Converts tree depth (int) to left-margin indentation width.</summary>
public sealed class DepthToIndentConverter : IValueConverter
{
    public static readonly DepthToIndentConverter Instance = new();
    private const double IndentWidth = 16.0;

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int depth ? depth * IndentWidth : 0.0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Maps FolderSyncState to a badge background colour.</summary>
public sealed class SyncStateToBadgeBackgroundConverter : IValueConverter
{
    public static readonly SyncStateToBadgeBackgroundConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is FolderSyncState state
            ? Color.Parse(state switch
            {
                FolderSyncState.Synced   => "#EAF3DE",
                FolderSyncState.Syncing  => "#E6F1FB",
                FolderSyncState.Included => "#E6F1FB",
                FolderSyncState.Partial  => "#FAEEDA",
                FolderSyncState.Conflict => "#FAEEDA",
                FolderSyncState.Error    => "#FCEBEB",
                _                        => "#F1EFE8"
            })
            : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Maps FolderSyncState to a badge text colour.</summary>
public sealed class SyncStateToBadgeForegroundConverter : IValueConverter
{
    public static readonly SyncStateToBadgeForegroundConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is FolderSyncState state
            ? Color.Parse(state switch
            {
                FolderSyncState.Synced   => "#27500A",
                FolderSyncState.Syncing  => "#0C447C",
                FolderSyncState.Included => "#0C447C",
                FolderSyncState.Partial  => "#633806",
                FolderSyncState.Conflict => "#633806",
                FolderSyncState.Error    => "#791F1F",
                _                        => "#5F5E5A"
            })
            : Colors.Transparent;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Converts bool IsIncluded to "Exclude" / "Include" button label.</summary>
public sealed class BoolToExcludeIncludeLabelConverter : IValueConverter
{
    public static readonly BoolToExcludeIncludeLabelConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "Exclude" : "Include";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Converts bool IsIncluded to tooltip text.</summary>
public sealed class BoolToExcludeIncludeTooltipConverter : IValueConverter
{
    public static readonly BoolToExcludeIncludeTooltipConverter Instance = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? "Exclude this folder from sync" : "Include this folder in sync";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
