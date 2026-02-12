using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Services;

/// <summary>
///     Service for managing window position and size preferences using database storage.
/// </summary>
public sealed class WindowPreferencesService(SyncDbContext context) : IWindowPreferencesService
{
    /// <inheritdoc />
    public async Task<WindowPreferences?> LoadAsync(CancellationToken cancellationToken = default)
    {
        WindowPreferencesEntity? entity = await context.WindowPreferences
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : MapToModel(entity);
    }

    /// <inheritdoc />
    public async Task SaveAsync(WindowPreferences preferences, CancellationToken cancellationToken = default)
    {
        WindowPreferencesEntity? entity = await context.WindowPreferences.FirstOrDefaultAsync(cancellationToken);

        if(entity is null)
        {
            entity = new WindowPreferencesEntity
            {
                X = preferences.X,
                Y = preferences.Y,
                Width = preferences.Width,
                Height = preferences.Height,
                IsMaximized = preferences.IsMaximized,
                Theme = preferences.Theme.ToString()
            };
            _ = context.WindowPreferences.Add(entity);
        }
        else
        {
            entity.X = preferences.X;
            entity.Y = preferences.Y;
            entity.Width = preferences.Width;
            entity.Height = preferences.Height;
            entity.IsMaximized = preferences.IsMaximized;
            entity.Theme = preferences.Theme.ToString();
        }

        _ = await context.SaveChangesAsync(cancellationToken);
    }

    private static WindowPreferences MapToModel(WindowPreferencesEntity entity)
    {
        // Parse theme with fallback to OriginalAuto for null, empty, or invalid values
        ThemePreference theme = ThemePreference.OriginalAuto;
        if (!string.IsNullOrWhiteSpace(entity.Theme) 
            && Enum.TryParse(entity.Theme, out ThemePreference parsedTheme))
        {
            theme = parsedTheme;
        }
        
        return new WindowPreferences(
            entity.Id,
            entity.X,
            entity.Y,
            entity.Width,
            entity.Height,
            entity.IsMaximized,
            theme
        );
    }
}
