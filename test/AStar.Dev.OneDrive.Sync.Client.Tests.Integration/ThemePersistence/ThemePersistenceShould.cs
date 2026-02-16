using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Integration.ThemePersistence;

public class ThemePersistenceShould : IAsyncDisposable
{
    private static readonly IModel ThemeModel = BuildThemeModel();
    private readonly SqliteConnection _connection;
    private readonly SyncDbContext _context = null!;

    public ThemePersistenceShould()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    [Fact]
    public async Task PersistThemeSelectionAcrossApplicationRestart()
    {
        await using(SyncDbContext context1 = CreateDbContext())
        {
            var preferencesService1 = new WindowPreferencesService(context1);
            var themeService1 = new ThemeService(preferencesService1);

            await themeService1.ApplyThemeAsync(ThemePreference.Professional, CancellationToken.None);

            themeService1.CurrentTheme.ShouldBe(ThemePreference.Professional);
        }

        await using SyncDbContext context2 = CreateDbContext();
        var preferencesService2 = new WindowPreferencesService(context2);

        WindowPreferences? loadedPreferences = await preferencesService2.LoadAsync(CancellationToken.None);
        _ = loadedPreferences.ShouldNotBeNull("preferences should exist in database");
        loadedPreferences.Theme.ShouldBe(ThemePreference.Professional, "theme selection should persist across application restarts");
    }

    [Theory]
    [InlineData(ThemePreference.OriginalLight)]
    [InlineData(ThemePreference.OriginalDark)]
    [InlineData(ThemePreference.Professional)]
    [InlineData(ThemePreference.Colourful)]
    [InlineData(ThemePreference.Terminal)]
    public async Task PersistAllThemeVariants(ThemePreference selectedTheme)
    {
        await using SyncDbContext context = CreateDbContext();
        var preferencesService = new WindowPreferencesService(context);
        var themeService = new ThemeService(preferencesService);
        await themeService.ApplyThemeAsync(selectedTheme, CancellationToken.None);

        await context.DisposeAsync();
        await using SyncDbContext context2 = CreateDbContext();
        var preferencesService2 = new WindowPreferencesService(context2);

        WindowPreferences? reloadedPreferences = await preferencesService2.LoadAsync(CancellationToken.None);

        _ = reloadedPreferences.ShouldNotBeNull();
        reloadedPreferences.Theme.ShouldBe(selectedTheme);
    }

    private SyncDbContext CreateDbContext()
    {
        DbContextOptions<SyncDbContext> options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseSqlite(_connection)
            .UseModel(ThemeModel)
            .Options;

        var context = new SyncDbContext(options);
        _ = context.Database.EnsureCreated();
        return context;
    }

    private static IModel BuildThemeModel()
    {
        var modelBuilder = new ModelBuilder(new ConventionSet());

        _ = modelBuilder.Entity<WindowPreferencesEntity>(entity =>
        {
            _ = entity.ToTable("WindowPreferences");
            _ = entity.HasKey(e => e.Id);
            _ = entity.Property(e => e.Id).ValueGeneratedOnAdd();
        });

        return (IModel)modelBuilder.Model;
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
