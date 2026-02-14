using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit.Services;

public class ThemeServiceShould
{
    private readonly IWindowPreferencesService _mockWindowPreferencesService;
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ThemeServiceShould() => _mockWindowPreferencesService = Substitute.For<IWindowPreferencesService>();

    [Fact]
    public async Task UpdateCurrentThemeProperty_WhenApplyThemeAsyncCalled()
    {
        var service = new ThemeService(_mockWindowPreferencesService);
        ThemePreference expectedTheme = ThemePreference.Professional;

        await service.ApplyThemeAsync(expectedTheme, CancellationToken);

        service.CurrentTheme.ShouldBe(expectedTheme);
    }

    [Fact]
    public async Task PersistToWindowPreferences_WhenApplyThemeAsyncCalled()
    {
        var service = new ThemeService(_mockWindowPreferencesService);
        ThemePreference theme = ThemePreference.Colourful;
        var existingPreferences = new WindowPreferences(
            Id: 1,
            X: 100,
            Y: 100,
            Width: 800,
            Height: 600,
            IsMaximized: false,
            Theme: ThemePreference.OriginalAuto);

        _ = _mockWindowPreferencesService.LoadAsync(Arg.Any<CancellationToken>())
            .Returns(existingPreferences);

        await service.ApplyThemeAsync(theme, CancellationToken);

        await _mockWindowPreferencesService.Received(1).SaveAsync(
            Arg.Is<WindowPreferences>(p => p.Theme == theme),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RaiseThemeChangedEvent_WhenApplyThemeAsyncCalled()
    {
        var service = new ThemeService(_mockWindowPreferencesService);
        ThemePreference theme = ThemePreference.Terminal;
        ThemePreference? raisedTheme = null;

        service.ThemeChanged += (sender, e) => raisedTheme = e;

        await service.ApplyThemeAsync(theme, CancellationToken);

        raisedTheme.ShouldBe(theme);
    }

    [Fact]
    public async Task InitializeCurrentThemeToOriginalAuto_WhenServiceCreated()
    {
        var service = new ThemeService(_mockWindowPreferencesService);

        service.CurrentTheme.ShouldBe(ThemePreference.OriginalAuto);
    }

    [Fact]
    public async Task ApplyOriginalAutoTheme_WhenCalledWithOriginalAuto()
    {
        var service = new ThemeService(_mockWindowPreferencesService);

        await service.ApplyThemeAsync(ThemePreference.OriginalAuto, CancellationToken);

        service.CurrentTheme.ShouldBe(ThemePreference.OriginalAuto);
    }

    [Fact]
    public async Task ApplyOriginalLightTheme_WhenCalledWithOriginalLight()
    {
        var service = new ThemeService(_mockWindowPreferencesService);

        await service.ApplyThemeAsync(ThemePreference.OriginalLight, CancellationToken);

        service.CurrentTheme.ShouldBe(ThemePreference.OriginalLight);
    }

    [Fact]
    public async Task ApplyOriginalDarkTheme_WhenCalledWithOriginalDark()
    {
        var service = new ThemeService(_mockWindowPreferencesService);

        await service.ApplyThemeAsync(ThemePreference.OriginalDark, CancellationToken);

        service.CurrentTheme.ShouldBe(ThemePreference.OriginalDark);
    }

    [Fact]
    public async Task ApplyProfessionalTheme_WhenCalledWithProfessional()
    {
        var service = new ThemeService(_mockWindowPreferencesService);

        await service.ApplyThemeAsync(ThemePreference.Professional, CancellationToken);

        service.CurrentTheme.ShouldBe(ThemePreference.Professional);
    }

    [Fact]
    public async Task ApplyColourfulTheme_WhenCalledWithColourful()
    {
        var service = new ThemeService(_mockWindowPreferencesService);

        await service.ApplyThemeAsync(ThemePreference.Colourful, CancellationToken);

        service.CurrentTheme.ShouldBe(ThemePreference.Colourful);
    }

    [Fact]
    public async Task ApplyTerminalTheme_WhenCalledWithTerminal()
    {
        var service = new ThemeService(_mockWindowPreferencesService);

        await service.ApplyThemeAsync(ThemePreference.Terminal, CancellationToken);

        service.CurrentTheme.ShouldBe(ThemePreference.Terminal);
    }

    [Fact]
    public async Task PreserveExistingWindowPreferences_WhenApplyingTheme()
    {
        var service = new ThemeService(_mockWindowPreferencesService);
        var existingPreferences = new WindowPreferences(
            Id: 1,
            X: 200,
            Y: 150,
            Width: 1024,
            Height: 768,
            IsMaximized: false,
            Theme: ThemePreference.OriginalAuto);

        _ = _mockWindowPreferencesService.LoadAsync(Arg.Any<CancellationToken>())
            .Returns(existingPreferences);

        await service.ApplyThemeAsync(ThemePreference.Professional, CancellationToken);

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
        var service = new ThemeService(_mockWindowPreferencesService);

        _ = _mockWindowPreferencesService.LoadAsync(Arg.Any<CancellationToken>())
            .Returns((WindowPreferences?)null);

        await service.ApplyThemeAsync(ThemePreference.Terminal, CancellationToken);

        await _mockWindowPreferencesService.Received(1).SaveAsync(
            Arg.Is<WindowPreferences>(p => p.Theme == ThemePreference.Terminal),
            Arg.Any<CancellationToken>());
    }
}
