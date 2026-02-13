using AStar.Dev.OneDrive.Client.Converters;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Client.Tests.Unit.Converters;

public class ThemePreferenceToDisplayNameConverterShould
{
    private readonly IValueConverter _sut = new ThemePreferenceToDisplayNameConverter();

    [Theory]
    [InlineData(ThemePreference.OriginalAuto, "Original (Automatic)")]
    [InlineData(ThemePreference.OriginalLight, "Original (Light)")]
    [InlineData(ThemePreference.OriginalDark, "Original (Dark)")]
    [InlineData(ThemePreference.Professional, "Professional")]
    [InlineData(ThemePreference.Colourful, "Colourful")]
    [InlineData(ThemePreference.Terminal, "Terminal / Hacker")]
    public void Convert_ReturnDisplayNameForThemePreference(ThemePreference theme, string expectedName)
    {
        var result = _sut.Convert(theme, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);

        result.ShouldBe(expectedName);
    }

    [Fact]
    public void Convert_ReturnUnknownForNullValue()
    {
        var result = _sut.Convert(null, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);

        result.ShouldBe("Unknown");
    }

    [Fact]
    public void ConvertBack_NotSupported()
    {
        var result = _sut.ConvertBack("Original (Automatic)", typeof(ThemePreference), null, System.Globalization.CultureInfo.CurrentCulture);

        result.ShouldBe(Avalonia.Data.BindingNotification.UnsetValue);
    }
}

