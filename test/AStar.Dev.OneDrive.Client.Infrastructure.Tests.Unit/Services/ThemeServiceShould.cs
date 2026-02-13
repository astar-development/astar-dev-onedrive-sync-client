using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using NSubstitute;
using Shouldly;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Services;

public class ThemeServiceShould
{
    private readonly IWindowPreferencesService _mockWindowPreferencesService;
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ThemeServiceShould()
    {
        _mockWindowPreferencesService = Substitute.For<IWindowPreferencesService>();
    }

    [Fact]
    public async Task UpdateCurrentThemeProperty_WhenApplyThemeAsyncCalled()
    {
        // Arrange
        var service = new ThemeService(_mockWindowPreferencesService);
        var expectedTheme = ThemePreference.Professional;

        // Act
        await service.ApplyThemeAsync(expectedTheme, CancellationToken);

        // Assert
        service.CurrentTheme.ShouldBe(expectedTheme);
    }

    [Fact]
    public async Task PersistToWindowPreferences_WhenApplyThemeAsyncCalled()
    {
        // Arrange
        var service = new ThemeService(_mockWindowPreferencesService);
        var theme = ThemePreference.Colourful;
        var existingPreferences = new WindowPreferences(
            Id: 1,
            X: 100,
            Y: 100,
            Width: 800,
            Height: 600,
            IsMaximized: false,
            Theme: ThemePreference.OriginalAuto);

        _mockWindowPreferencesService.LoadAsync(Arg.Any<CancellationToken>())
            .Returns(existingPreferences);

        // Act
        await service.ApplyThemeAsync(theme, CancellationToken);

        // Assert
        await _mockWindowPreferencesService.Received(1).SaveAsync(
            Arg.Is<WindowPreferences>(p => p.Theme == theme),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RaiseThemeChangedEvent_WhenApplyThemeAsyncCalled()
    {
        // Arrange
        var service = new ThemeService(_mockWindowPreferencesService);
        var theme = ThemePreference.Terminal;
        ThemePreference? raisedTheme = null;

        service.ThemeChanged += (sender, e) => raisedTheme = e;

        // Act
        await service.ApplyThemeAsync(theme, CancellationToken);

        // Assert
        raisedTheme.ShouldBe(theme);
    }

    [Fact]
    public async Task InitializeCurrentThemeToOriginalAuto_WhenServiceCreated()
    {
        // Arrange & Act
        var service = new ThemeService(_mockWindowPreferencesService);

        // Assert
        service.CurrentTheme.ShouldBe(ThemePreference.OriginalAuto);
    }

    [Fact]
    public async Task ApplyOriginalAutoTheme_WhenCalledWithOriginalAuto()
    {
        // Arrange
        var service = new ThemeService(_mockWindowPreferencesService);
        
        // Act
        await service.ApplyThemeAsync(ThemePreference.OriginalAuto, CancellationToken);

        // Assert
        service.CurrentTheme.ShouldBe(ThemePreference.OriginalAuto);
        // Note: Testing Application.Current.RequestedThemeVariant requires integration test
    }

    [Fact]
    public async Task ApplyOriginalLightTheme_WhenCalledWithOriginalLight()
    {
        // Arrange
        var service = new ThemeService(_mockWindowPreferencesService);

        // Act
        await service.ApplyThemeAsync(ThemePreference.OriginalLight, CancellationToken);

        // Assert
        service.CurrentTheme.ShouldBe(ThemePreference.OriginalLight);
        // Note: Testing Application.Current.RequestedThemeVariant requires integration test
    }

    [Fact]
    public async Task ApplyOriginalDarkTheme_WhenCalledWithOriginalDark()
    {
        // Arrange
        var service = new ThemeService(_mockWindowPreferencesService);

        // Act
        await service.ApplyThemeAsync(ThemePreference.OriginalDark, CancellationToken);

        // Assert
        service.CurrentTheme.ShouldBe(ThemePreference.OriginalDark);
        // Note: Testing Application.Current.RequestedThemeVariant requires integration test
    }

    [Fact]
    public async Task ApplyProfessionalTheme_WhenCalledWithProfessional()
    {
        // Arrange
        var service = new ThemeService(_mockWindowPreferencesService);

        // Act
        await service.ApplyThemeAsync(ThemePreference.Professional, CancellationToken);

        // Assert
        service.CurrentTheme.ShouldBe(ThemePreference.Professional);
        // Note: Testing ResourceDictionary loading requires integration test or Avalonia test host
    }

    [Fact]
    public async Task ApplyColourfulTheme_WhenCalledWithColourful()
    {
        // Arrange
        var service = new ThemeService(_mockWindowPreferencesService);

        // Act
        await service.ApplyThemeAsync(ThemePreference.Colourful, CancellationToken);

        // Assert
        service.CurrentTheme.ShouldBe(ThemePreference.Colourful);
        // Note: Testing ResourceDictionary loading requires integration test or Avalonia test host
    }

    [Fact]
    public async Task ApplyTerminalTheme_WhenCalledWithTerminal()
    {
        // Arrange
        var service = new ThemeService(_mockWindowPreferencesService);

        // Act
        await service.ApplyThemeAsync(ThemePreference.Terminal, CancellationToken);

        // Assert
        service.CurrentTheme.ShouldBe(ThemePreference.Terminal);
        // Note: Testing ResourceDictionary loading requires integration test or Avalonia test host
    }

    [Fact]
    public async Task PreserveExistingWindowPreferences_WhenApplyingTheme()
    {
        // Arrange
        var service = new ThemeService(_mockWindowPreferencesService);
        var existingPreferences = new WindowPreferences(
            Id: 1,
            X: 200,
            Y: 150,
            Width: 1024,
            Height: 768,
            IsMaximized: false,
            Theme: ThemePreference.OriginalAuto);

        _mockWindowPreferencesService.LoadAsync(Arg.Any<CancellationToken>())
            .Returns(existingPreferences);

        // Act
        await service.ApplyThemeAsync(ThemePreference.Professional, CancellationToken);

        // Assert
        await _mockWindowPreferencesService.Received(1).SaveAsync(
            Arg.Is<WindowPreferences>(p =>
                p.X == 200 &&
                p.Y == 150 &&
                p.Width == 1024 &&
                p.Height == 768 &&
                p.Theme == ThemePreference.Professional),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateDefaultPreferences_WhenNoneExist()
    {
        // Arrange
        var service = new ThemeService(_mockWindowPreferencesService);
        
        _mockWindowPreferencesService.LoadAsync(Arg.Any<CancellationToken>())
            .Returns((WindowPreferences?)null);

        // Act
        await service.ApplyThemeAsync(ThemePreference.Terminal, CancellationToken);

        // Assert
        await _mockWindowPreferencesService.Received(1).SaveAsync(
            Arg.Is<WindowPreferences>(p => p.Theme == ThemePreference.Terminal),
            Arg.Any<CancellationToken>());
    }
}
