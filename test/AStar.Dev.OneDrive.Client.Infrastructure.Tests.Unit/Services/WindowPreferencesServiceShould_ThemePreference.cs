using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Services;

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
        // Arrange
        WindowPreferences preferences = new(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1200,
            Height: 800,
            IsMaximized: false,
            Theme: ThemePreference.OriginalAuto
        );

        // Act
        await _sut.SaveAsync(preferences);
        WindowPreferences? loaded = await _sut.LoadAsync();

        // Assert
        loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.OriginalAuto);
    }

    [Fact]
    public async Task SaveAndLoadThemePreference_PersistsOriginalLightCorrectly()
    {
        // Arrange
        WindowPreferences preferences = new(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1200,
            Height: 800,
            IsMaximized: false,
            Theme: ThemePreference.OriginalLight
        );

        // Act
        await _sut.SaveAsync(preferences);
        WindowPreferences? loaded = await _sut.LoadAsync();

        // Assert
        loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.OriginalLight);
    }

    [Fact]
    public async Task SaveAndLoadThemePreference_PersistsOriginalDarkCorrectly()
    {
        // Arrange
        WindowPreferences preferences = new(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1200,
            Height: 800,
            IsMaximized: false,
            Theme: ThemePreference.OriginalDark
        );

        // Act
        await _sut.SaveAsync(preferences);
        WindowPreferences? loaded = await _sut.LoadAsync();

        // Assert
        loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.OriginalDark);
    }

    [Fact]
    public async Task SaveAndLoadThemePreference_PersistsProfessionalCorrectly()
    {
        // Arrange
        WindowPreferences preferences = new(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1200,
            Height: 800,
            IsMaximized: false,
            Theme: ThemePreference.Professional
        );

        // Act
        await _sut.SaveAsync(preferences);
        WindowPreferences? loaded = await _sut.LoadAsync();

        // Assert
        loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.Professional);
    }

    [Fact]
    public async Task SaveAndLoadThemePreference_PersistsColourfulCorrectly()
    {
        // Arrange
        WindowPreferences preferences = new(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1200,
            Height: 800,
            IsMaximized: false,
            Theme: ThemePreference.Colourful
        );

        // Act
        await _sut.SaveAsync(preferences);
        WindowPreferences? loaded = await _sut.LoadAsync();

        // Assert
        loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.Colourful);
    }

    [Fact]
    public async Task SaveAndLoadThemePreference_PersistsTerminalCorrectly()
    {
        // Arrange
        WindowPreferences preferences = new(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1200,
            Height: 800,
            IsMaximized: false,
            Theme: ThemePreference.Terminal
        );

        // Act
        await _sut.SaveAsync(preferences);
        WindowPreferences? loaded = await _sut.LoadAsync();

        // Assert
        loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.Terminal);
    }

    [Fact]
    public async Task LoadThemePreference_DefaultsToOriginalAuto_WhenNull()
    {
        // Arrange - manually create entity with null theme
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
        _context.WindowPreferences.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        WindowPreferences? loaded = await _sut.LoadAsync();

        // Assert
        loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.OriginalAuto);
    }

    [Fact]
    public async Task LoadThemePreference_DefaultsToOriginalAuto_WhenEmpty()
    {
        // Arrange - manually create entity with empty theme
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
        _context.WindowPreferences.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        WindowPreferences? loaded = await _sut.LoadAsync();

        // Assert
        loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.OriginalAuto);
    }

    [Fact]
    public async Task LoadThemePreference_DefaultsToOriginalAuto_WhenInvalid()
    {
        // Arrange - manually create entity with invalid theme value
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
        _context.WindowPreferences.Add(entity);
        await _context.SaveChangesAsync();

        // Act
        WindowPreferences? loaded = await _sut.LoadAsync();

        // Assert
        loaded.ShouldNotBeNull();
        loaded!.Theme.ShouldBe(ThemePreference.OriginalAuto);
    }

    public void Dispose()
    {
        _context?.Dispose();
        GC.SuppressFinalize(this);
    }
}
