using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Client.Settings;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Settings;

public class SettingsViewModelShould
{
    private readonly IThemeService _mockThemeService = Substitute.For<IThemeService>();

    public SettingsViewModelShould() => _ = _mockThemeService.CurrentTheme.Returns(ThemePreference.OriginalAuto);

    [Fact]
    public void InitializeSelectedThemeFromThemeServiceCurrentTheme()
    {
        _ = _mockThemeService.CurrentTheme.Returns(ThemePreference.Professional);

        var sut = new SettingsViewModel(_mockThemeService);

        sut.SelectedTheme.ShouldBe(ThemePreference.Professional);
    }

    [Fact]
    public void ProvideAvailableThemesContainingAllSixOptions()
    {
        var sut = new SettingsViewModel(_mockThemeService);

        var availableThemes = sut.AvailableThemes.ToList();
        availableThemes.Count.ShouldBe(6);
        availableThemes.ShouldContain(ThemePreference.OriginalAuto);
        availableThemes.ShouldContain(ThemePreference.OriginalLight);
        availableThemes.ShouldContain(ThemePreference.OriginalDark);
        availableThemes.ShouldContain(ThemePreference.Professional);
        availableThemes.ShouldContain(ThemePreference.Colourful);
        availableThemes.ShouldContain(ThemePreference.Terminal);
    }

    [Fact]
    public void ApplyThemeCommand_IsNotNull()
    {
        var sut = new SettingsViewModel(_mockThemeService);

        _ = sut.ApplyThemeCommand.ShouldNotBeNull();
    }

    [Fact]
    public void ApplyThemeCommand_CanExecute()
    {
        var sut = new SettingsViewModel(_mockThemeService);

        sut.ApplyThemeCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public void SelectedTheme_CanBeChanged()
    {
        var sut = new SettingsViewModel(_mockThemeService)
        {
            SelectedTheme = ThemePreference.Colourful
        };

        sut.SelectedTheme.ShouldBe(ThemePreference.Colourful);
    }

    [Fact]
    public void StatusMessage_InitiallyNull()
    {
        var sut = new SettingsViewModel(_mockThemeService);

        sut.StatusMessage.ShouldBeNull();
    }

    [Fact]
    public void StatusMessage_CanBeSet()
    {
        var sut = new SettingsViewModel(_mockThemeService)
        {
            StatusMessage = "Test message"
        };

        sut.StatusMessage.ShouldBe("Test message");
    }
}
