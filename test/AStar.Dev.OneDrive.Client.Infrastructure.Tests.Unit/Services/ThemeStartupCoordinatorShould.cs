namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Services;

using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using NSubstitute;
using Xunit;

public class ThemeStartupCoordinatorShould
{
    private readonly IThemeService _mockThemeService = Substitute.For<IThemeService>();
    private readonly IWindowPreferencesService _mockWindowPreferencesService = Substitute.For<IWindowPreferencesService>();
    private readonly ThemeStartupCoordinator _sut;
    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public ThemeStartupCoordinatorShould()
    {
        _sut = new ThemeStartupCoordinator(_mockThemeService, _mockWindowPreferencesService);
    }

    [Fact]
    public async Task ApplySavedTheme_WhenPreferencesExist()
    {
        // Arrange
        var savedPreferences = new WindowPreferences(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1024,
            Height: 768,
            IsMaximized: false,
            Theme: ThemePreference.Professional);
        _mockWindowPreferencesService.LoadAsync(CancellationToken)
            .Returns(Task.FromResult((WindowPreferences?)savedPreferences));

        // Act
        await _sut.InitializeThemeOnStartupAsync(CancellationToken);

        // Assert
        await _mockThemeService.Received(1).ApplyThemeAsync(ThemePreference.Professional, CancellationToken);
    }

    [Fact]
    public async Task ApplyDefaultTheme_WhenNoPreferencesExist()
    {
        // Arrange
        _mockWindowPreferencesService.LoadAsync(CancellationToken)
            .Returns(Task.FromResult((WindowPreferences?)null));

        // Act
        await _sut.InitializeThemeOnStartupAsync(CancellationToken);

        // Assert
        await _mockThemeService.Received(1).ApplyThemeAsync(ThemePreference.OriginalAuto, CancellationToken);
    }

    [Fact]
    public async Task ApplyLoadedTheme_WhenPreferencesExist()
    {
        // Arrange
        var savedPreferences = new WindowPreferences(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1024,
            Height: 768,
            IsMaximized: false,
            Theme: ThemePreference.Colourful);
        _mockWindowPreferencesService.LoadAsync(CancellationToken)
            .Returns(Task.FromResult((WindowPreferences?)savedPreferences));

        // Act
        await _sut.InitializeThemeOnStartupAsync(CancellationToken);

        // Assert
        await _mockThemeService.Received(1).ApplyThemeAsync(ThemePreference.Colourful, CancellationToken);
    }

    [Fact]
    public async Task NotThrowException_WhenThemeServiceFails()
    {
        // Arrange
        var savedPreferences = new WindowPreferences(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1024,
            Height: 768,
            IsMaximized: false,
            Theme: ThemePreference.Terminal);
        _mockWindowPreferencesService.LoadAsync(CancellationToken)
            .Returns(Task.FromResult((WindowPreferences?)savedPreferences));
        _mockThemeService.ApplyThemeAsync(Arg.Any<ThemePreference>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Theme load failed"));

        // Act & Assert
        var exception = await Record.ExceptionAsync(() =>
            _sut.InitializeThemeOnStartupAsync(CancellationToken));

        // Assert: No exception should propagate
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
        // Arrange
        var preferences = new WindowPreferences(
            Id: 1,
            X: 100,
            Y: 200,
            Width: 1024,
            Height: 768,
            IsMaximized: false,
            Theme: themePreference);
        _mockWindowPreferencesService.LoadAsync(CancellationToken)
            .Returns(Task.FromResult((WindowPreferences?)preferences));

        // Act
        await _sut.InitializeThemeOnStartupAsync(CancellationToken);

        // Assert
        await _mockThemeService.Received(1).ApplyThemeAsync(expectedTheme, CancellationToken);
    }
}
