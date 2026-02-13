using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Tests.Integration.ThemePersistence;

/// <summary>
/// Integration test to verify theme selection persists across application restarts.
/// This test reproduces the bug reported in phase 5.5 manual testing.
/// </summary>
public class ThemePersistenceShould : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private SyncDbContext _context = null!;

    public ThemePersistenceShould()
    {
        // Setup in-memory SQLite database
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task PersistThemeSelectionAcrossApplicationRestart()
    {
        // Arrange - Simulate first application session
        await using(var context1 = CreateDbContext())
        {
            var preferencesService1 = new WindowPreferencesService(context1);
            var themeService1 = new ThemeService(preferencesService1);

            // Act - User selects Professional theme and applies it
            await themeService1.ApplyThemeAsync(ThemePreference.Professional, CancellationToken.None);

            // Verify theme was applied in memory
            themeService1.CurrentTheme.ShouldBe(ThemePreference.Professional);
        }

        // Simulate application restart - create new context and services
        await using(var context2 = CreateDbContext())
        {
            var preferencesService2 = new WindowPreferencesService(context2);

            // Act - Load preferences after restart
            var loadedPreferences = await preferencesService2.LoadAsync(CancellationToken.None);

            // Assert - Theme should have persisted to database
            loadedPreferences.ShouldNotBeNull("preferences should exist in database");
            loadedPreferences.Theme.ShouldBe(ThemePreference.Professional,
                "theme selection should persist across application restarts");
        }
    }

    [Theory]
    [InlineData(ThemePreference.OriginalLight)]
    [InlineData(ThemePreference.OriginalDark)]
    [InlineData(ThemePreference.Professional)]
    [InlineData(ThemePreference.Colourful)]
    [InlineData(ThemePreference.Terminal)]
    public async Task PersistAllThemeVariants(ThemePreference selectedTheme)
    {
        // Arrange
        await using var context = CreateDbContext();
        var preferencesService = new WindowPreferencesService(context);
        var themeService = new ThemeService(preferencesService);

        // Act - Apply theme
        await themeService.ApplyThemeAsync(selectedTheme, CancellationToken.None);

        // Force a new context to simulate restart
        await context.DisposeAsync();
        await using var context2 = CreateDbContext();
        var preferencesService2 = new WindowPreferencesService(context2);

        // Act - Reload
        var reloadedPreferences = await preferencesService2.LoadAsync(CancellationToken.None);

        // Assert
        reloadedPreferences.ShouldNotBeNull();
        reloadedPreferences.Theme.ShouldBe(selectedTheme);
    }

    private SyncDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseSqlite(_connection)
            .Options;

        var context = new SyncDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public async ValueTask DisposeAsync()
    {
        if(_context is not null)
        {
            await _context.DisposeAsync();
        }
        await _connection.DisposeAsync();
    }
}
