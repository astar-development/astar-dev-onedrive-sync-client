using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Client.Settings;
using NSubstitute;
using Shouldly;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Settings;

public class SettingsViewModelShould
{
    private readonly IThemeService _mockThemeService = Substitute.For<IThemeService>();

    public SettingsViewModelShould()
    {
        _mockThemeService.CurrentTheme.Returns(ThemePreference.OriginalAuto);
    }

    [Fact]
    public void InitializeSelectedThemeFromThemeServiceCurrentTheme()
    {
        // Arrange
        _mockThemeService.CurrentTheme.Returns(ThemePreference.Professional);

        // Act
        var sut = new SettingsViewModel(_mockThemeService);

        // Assert
        sut.SelectedTheme.ShouldBe(ThemePreference.Professional);
    }

    [Fact]
    public void ProvideAvailableThemesContainingAllSixOptions()
    {
        // Arrange & Act
        var sut = new SettingsViewModel(_mockThemeService);

        // Assert
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
        // Arrange & Act
        var sut = new SettingsViewModel(_mockThemeService);

        // Assert
        sut.ApplyThemeCommand.ShouldNotBeNull();
    }

    [Fact]
    public void ApplyThemeCommand_CanExecute()
    {
        // Arrange & Act
        var sut = new SettingsViewModel(_mockThemeService);

        // Assert
        sut.ApplyThemeCommand.CanExecute(null).ShouldBeTrue();
    }

    [Fact]
    public void SelectedTheme_CanBeChanged()
    {
        // Arrange
        var sut = new SettingsViewModel(_mockThemeService);

        // Act
        sut.SelectedTheme = ThemePreference.Colourful;

        // Assert
        sut.SelectedTheme.ShouldBe(ThemePreference.Colourful);
    }

    [Fact]
    public void StatusMessage_InitiallyNull()
    {
        // Arrange & Act
        var sut = new SettingsViewModel(_mockThemeService);

        // Assert
        sut.StatusMessage.ShouldBeNull();
    }

    [Fact]
    public void StatusMessage_CanBeSet()
    {
        // Arrange
        var sut = new SettingsViewModel(_mockThemeService);

        // Act
        sut.StatusMessage = "Test message";

        // Assert
        sut.StatusMessage.ShouldBe("Test message");
    }
}
