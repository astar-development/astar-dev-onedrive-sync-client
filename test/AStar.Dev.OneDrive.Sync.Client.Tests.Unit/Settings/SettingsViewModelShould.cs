using System.Windows.Input;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Sync.Client.Settings;
using NSubstitute;
using Shouldly;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Settings;

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

    [Fact]
    public async Task CallThemeServiceWhenApplyThemeCommandExecuted()
    {
        _ = _mockThemeService.ApplyThemeAsync(Arg.Any<ThemePreference>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new SettingsViewModel(_mockThemeService)
        {
            SelectedTheme = ThemePreference.Professional
        };

        await Task.Delay(100, TestContext.Current.CancellationToken);

        if(sut.ApplyThemeCommand.CanExecute(null))
        {
            ((ICommand)sut.ApplyThemeCommand).Execute(null);
        }

        await Task.Delay(100, TestContext.Current.CancellationToken);

        await _mockThemeService.Received(2).ApplyThemeAsync(Arg.Any<ThemePreference>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetStatusMessageWhenThemeAppliedSuccessfully()
    {
        _ = _mockThemeService.ApplyThemeAsync(Arg.Any<ThemePreference>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new SettingsViewModel(_mockThemeService)
        {
            SelectedTheme = ThemePreference.Colourful
        };

        await Task.Delay(100, TestContext.Current.CancellationToken);

        if(sut.ApplyThemeCommand.CanExecute(null))
        {
            ((ICommand)sut.ApplyThemeCommand).Execute(null);
        }

        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.StatusMessage.ShouldBe("Theme changed to Colourful");
    }

    [Fact]
    public async Task SetStatusMessageToNullWhenThemeApplicationFails()
    {
        _ = _mockThemeService.ApplyThemeAsync(Arg.Any<ThemePreference>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Theme service error")));

        var sut = new SettingsViewModel(_mockThemeService);

        sut.StatusMessage = "Initial message";

        if(sut.ApplyThemeCommand.CanExecute(null))
        {
            ((ICommand)sut.ApplyThemeCommand).Execute(null);
        }

        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.StatusMessage.ShouldBeNull();
    }

    [Fact]
    public async Task AutoApplyThemeWhenSelectedThemeChanges()
    {
        _ = _mockThemeService.ApplyThemeAsync(Arg.Any<ThemePreference>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new SettingsViewModel(_mockThemeService);

        await Task.Delay(100, TestContext.Current.CancellationToken);

        _mockThemeService.ClearReceivedCalls();

        sut.SelectedTheme = ThemePreference.Terminal;

        await Task.Delay(200, TestContext.Current.CancellationToken);

        await _mockThemeService.Received(1).ApplyThemeAsync(ThemePreference.Terminal, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NotAutoApplyThemeOnInitialValue()
    {
        _ = _mockThemeService.ApplyThemeAsync(Arg.Any<ThemePreference>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new SettingsViewModel(_mockThemeService);

        await Task.Delay(100, TestContext.Current.CancellationToken);

        await _mockThemeService.DidNotReceive().ApplyThemeAsync(Arg.Any<ThemePreference>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateSelectedThemeWhenThemeChangedEventRaised()
    {
        var sut = new SettingsViewModel(_mockThemeService);

        sut.SelectedTheme.ShouldBe(ThemePreference.OriginalAuto);

        _ = _mockThemeService.CurrentTheme.Returns(ThemePreference.Professional);

        _mockThemeService.ThemeChanged += Raise.Event<EventHandler<ThemePreference>>(this, ThemePreference.Professional);

        await Task.Delay(50, TestContext.Current.CancellationToken);

        sut.SelectedTheme.ShouldBe(ThemePreference.Professional);
    }

    [Fact]
    public void RaisePropertyChangedWhenSelectedThemeChanges()
    {
        var sut = new SettingsViewModel(_mockThemeService);
        var propertyChangedRaised = false;
        var propertyNames = new List<string>();

        sut.PropertyChanged += (_, e) =>
        {
            if(e.PropertyName == nameof(SettingsViewModel.SelectedTheme))
            {
                propertyChangedRaised = true;
            }
            if(e.PropertyName is not null)
            {
                propertyNames.Add(e.PropertyName);
            }
        };

        sut.SelectedTheme = ThemePreference.Colourful;

        propertyChangedRaised.ShouldBeTrue();
        propertyNames.ShouldContain(nameof(SettingsViewModel.SelectedTheme));
    }

    [Fact]
    public void RaisePropertyChangedWhenStatusMessageChanges()
    {
        var sut = new SettingsViewModel(_mockThemeService);
        var propertyChangedRaised = false;
        string? propertyName = null;

        sut.PropertyChanged += (_, e) =>
        {
            propertyChangedRaised = true;
            propertyName = e.PropertyName;
        };

        sut.StatusMessage = "Theme applied";

        propertyChangedRaised.ShouldBeTrue();
        propertyName.ShouldBe(nameof(SettingsViewModel.StatusMessage));
    }

    [Fact]
    public async Task CallThemeServiceWithCorrectThemeParameter()
    {
        _ = _mockThemeService.ApplyThemeAsync(Arg.Any<ThemePreference>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var sut = new SettingsViewModel(_mockThemeService)
        {
            SelectedTheme = ThemePreference.Terminal
        };

        await Task.Delay(100, TestContext.Current.CancellationToken);

        if(sut.ApplyThemeCommand.CanExecute(null))
        {
            ((ICommand)sut.ApplyThemeCommand).Execute(null);
        }

        await Task.Delay(100, TestContext.Current.CancellationToken);

        await _mockThemeService.Received().ApplyThemeAsync(ThemePreference.Terminal, Arg.Any<CancellationToken>());
    }
}
