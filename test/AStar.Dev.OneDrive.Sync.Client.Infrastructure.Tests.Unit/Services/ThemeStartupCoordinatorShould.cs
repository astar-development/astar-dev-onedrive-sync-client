
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit.Services;

public class ThemeStartupCoordinatorShould
{
    private readonly IThemeService _mockThemeService = Substitute.For<IThemeService>();
    private readonly IWindowPreferencesService _mockWindowPreferencesService = Substitute.For<IWindowPreferencesService>();
    private readonly ThemeStartupCoordinator _sut;
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ThemeStartupCoordinatorShould() => _sut = new ThemeStartupCoordinator(_mockThemeService, _mockWindowPreferencesService);

    [Fact]
    public async Task ApplySavedTheme_WhenPreferencesExist()
    {
        var savedPreferences = new WindowPreferences(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1024,
            Height: 768,
            IsMaximized: false,
            Theme: ThemePreference.Professional);
        _ = _mockWindowPreferencesService.LoadAsync(CancellationToken)
            .Returns(Task.FromResult((WindowPreferences?)savedPreferences));

        await _sut.InitializeThemeOnStartupAsync(CancellationToken);

        await _mockThemeService.Received(1).ApplyThemeAsync(ThemePreference.Professional, CancellationToken);
    }

    [Fact]
    public async Task ApplyDefaultTheme_WhenNoPreferencesExist()
    {
        _ = _mockWindowPreferencesService.LoadAsync(CancellationToken)
            .Returns(Task.FromResult((WindowPreferences?)null));

        await _sut.InitializeThemeOnStartupAsync(CancellationToken);

        await _mockThemeService.Received(1).ApplyThemeAsync(ThemePreference.OriginalAuto, CancellationToken);
    }

    [Fact]
    public async Task ApplyLoadedTheme_WhenPreferencesExist()
    {
        var savedPreferences = new WindowPreferences(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1024,
            Height: 768,
            IsMaximized: false,
            Theme: ThemePreference.Colourful);
        _ = _mockWindowPreferencesService.LoadAsync(CancellationToken)
            .Returns(Task.FromResult((WindowPreferences?)savedPreferences));

        await _sut.InitializeThemeOnStartupAsync(CancellationToken);

        await _mockThemeService.Received(1).ApplyThemeAsync(ThemePreference.Colourful, CancellationToken);
    }

    [Fact]
    public async Task NotThrowException_WhenThemeServiceFails()
    {
        var savedPreferences = new WindowPreferences(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1024,
            Height: 768,
            IsMaximized: false,
            Theme: ThemePreference.Terminal);
        _ = _mockWindowPreferencesService.LoadAsync(CancellationToken)
            .Returns(Task.FromResult((WindowPreferences?)savedPreferences));
        _ = _mockThemeService.ApplyThemeAsync(Arg.Any<ThemePreference>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Theme load failed")));

        Exception? exception = await Record.ExceptionAsync(() =>
            _sut.InitializeThemeOnStartupAsync(CancellationToken));
        exception.ShouldBeNull();
    }

    [Theory]
    [InlineData(ThemePreference.OriginalAuto, ThemePreference.OriginalAuto)]
    [InlineData(ThemePreference.OriginalLight, ThemePreference.OriginalLight)]
    [InlineData(ThemePreference.OriginalDark, ThemePreference.OriginalDark)]
    [InlineData(ThemePreference.Professional, ThemePreference.Professional)]
    [InlineData(ThemePreference.Colourful, ThemePreference.Colourful)]
    [InlineData(ThemePreference.Terminal, ThemePreference.Terminal)]
    public async Task ApplyCorrectThemeVariant(ThemePreference themePreference, ThemePreference expectedTheme)
    {
        var preferences = new WindowPreferences(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1024,
            Height: 768,
            IsMaximized: false,
            Theme: themePreference);
        _ = _mockWindowPreferencesService.LoadAsync(CancellationToken)
            .Returns(Task.FromResult((WindowPreferences?)preferences));

        await _sut.InitializeThemeOnStartupAsync(CancellationToken);

        await _mockThemeService.Received(1).ApplyThemeAsync(expectedTheme, CancellationToken);
    }
}
