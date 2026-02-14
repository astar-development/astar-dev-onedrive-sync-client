using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit.Services;

public sealed class WindowPreferencesServiceShould_ThemePreference : IDisposable
{
    private readonly SyncDbContext _context;
    private readonly WindowPreferencesService _sut;

    public WindowPreferencesServiceShould_ThemePreference()
    {
        DbContextOptions<SyncDbContext> options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        _context = new SyncDbContext(options);
        _sut = new WindowPreferencesService(_context);
    }

    [Fact]
    public async Task SaveAndLoadThemePreference_PersistsOriginalAutoCorrectly()
    {
        WindowPreferences preferences = new(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1200,
            Height: 800,
            IsMaximized: false,
            Theme: ThemePreference.OriginalAuto
        );

        await _sut.SaveAsync(preferences, TestContext.Current.CancellationToken);
        WindowPreferences? loaded = await _sut.LoadAsync(TestContext.Current.CancellationToken);

        _ = loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.OriginalAuto);
    }

    [Fact]
    public async Task SaveAndLoadThemePreference_PersistsOriginalLightCorrectly()
    {
        WindowPreferences preferences = new(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1200,
            Height: 800,
            IsMaximized: false,
            Theme: ThemePreference.OriginalLight
        );

        await _sut.SaveAsync(preferences, TestContext.Current.CancellationToken);
        WindowPreferences? loaded = await _sut.LoadAsync(TestContext.Current.CancellationToken);

        _ = loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.OriginalLight);
    }

    [Fact]
    public async Task SaveAndLoadThemePreference_PersistsOriginalDarkCorrectly()
    {
        WindowPreferences preferences = new(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1200,
            Height: 800,
            IsMaximized: false,
            Theme: ThemePreference.OriginalDark
        );

        await _sut.SaveAsync(preferences, TestContext.Current.CancellationToken);
        WindowPreferences? loaded = await _sut.LoadAsync(TestContext.Current.CancellationToken);

        _ = loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.OriginalDark);
    }

    [Fact]
    public async Task SaveAndLoadThemePreference_PersistsProfessionalCorrectly()
    {
        WindowPreferences preferences = new(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1200,
            Height: 800,
            IsMaximized: false,
            Theme: ThemePreference.Professional
        );

        await _sut.SaveAsync(preferences, TestContext.Current.CancellationToken);
        WindowPreferences? loaded = await _sut.LoadAsync(TestContext.Current.CancellationToken);

        _ = loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.Professional);
    }

    [Fact]
    public async Task SaveAndLoadThemePreference_PersistsColourfulCorrectly()
    {
        WindowPreferences preferences = new(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1200,
            Height: 800,
            IsMaximized: false,
            Theme: ThemePreference.Colourful
        );

        await _sut.SaveAsync(preferences, TestContext.Current.CancellationToken);
        WindowPreferences? loaded = await _sut.LoadAsync(TestContext.Current.CancellationToken);

        _ = loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.Colourful);
    }

    [Fact]
    public async Task SaveAndLoadThemePreference_PersistsTerminalCorrectly()
    {
        WindowPreferences preferences = new(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1200,
            Height: 800,
            IsMaximized: false,
            Theme: ThemePreference.Terminal
        );

        await _sut.SaveAsync(preferences, TestContext.Current.CancellationToken);
        WindowPreferences? loaded = await _sut.LoadAsync(TestContext.Current.CancellationToken);

        _ = loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.Terminal);
    }

    [Fact]
    public async Task LoadThemePreference_DefaultsToOriginalAuto_WhenNull()
    {
        WindowPreferencesEntity entity = new()
        {
            Id = 1,
            X = 100,
            Y = 200,
            Width = 1200,
            Height = 800,
            IsMaximized = false,
            Theme = null
        };
        _ = _context.WindowPreferences.Add(entity);
        _ = await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        WindowPreferences? loaded = await _sut.LoadAsync(TestContext.Current.CancellationToken);

        _ = loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.OriginalAuto);
    }

    [Fact]
    public async Task LoadThemePreference_DefaultsToOriginalAuto_WhenEmpty()
    {
        WindowPreferencesEntity entity = new()
        {
            Id = 1,
            X = 100,
            Y = 200,
            Width = 1200,
            Height = 800,
            IsMaximized = false,
            Theme = string.Empty
        };
        _ = _context.WindowPreferences.Add(entity);
        _ = await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        WindowPreferences? loaded = await _sut.LoadAsync(TestContext.Current.CancellationToken);

        _ = loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.OriginalAuto);
    }

    [Fact]
    public async Task LoadThemePreference_DefaultsToOriginalAuto_WhenInvalid()
    {
        WindowPreferencesEntity entity = new()
        {
            Id = 1,
            X = 100,
            Y = 200,
            Width = 1200,
            Height = 800,
            IsMaximized = false,
            Theme = "InvalidThemeValue"
        };
        _ = _context.WindowPreferences.Add(entity);
        _ = await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        WindowPreferences? loaded = await _sut.LoadAsync(TestContext.Current.CancellationToken);

        _ = loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.OriginalAuto);
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
