using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Converters;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Converters;

public class EnumToBooleanConverterShould
{
    private readonly IValueConverter _converter;

    public EnumToBooleanConverterShould() => _converter = new EnumToBooleanConverter();

    [Theory]
    [InlineData(ThemePreference.OriginalAuto, ThemePreference.OriginalAuto, true)]
    [InlineData(ThemePreference.OriginalAuto, ThemePreference.OriginalLight, false)]
    [InlineData(ThemePreference.OriginalLight, ThemePreference.OriginalLight, true)]
    [InlineData(ThemePreference.OriginalDark, ThemePreference.OriginalDark, true)]
    [InlineData(ThemePreference.Professional, ThemePreference.Colourful, false)]
    public void ReturnTrueWhenEnumValueMatchesParameter(ThemePreference value, ThemePreference parameter, bool expected)
    {
        var result = _converter.Convert(value, typeof(bool), parameter, CultureInfo.InvariantCulture);

        result.ShouldBe(expected);
    }

    [Fact]
    public void ReturnFalseWhenValueIsNull()
    {
        var result = _converter.Convert(null, typeof(bool), ThemePreference.OriginalAuto, CultureInfo.InvariantCulture);

        result.ShouldBe(false);
    }

    [Fact]
    public void ReturnFalseWhenParameterIsNull()
    {
        var result = _converter.Convert(ThemePreference.OriginalAuto, typeof(bool), null, CultureInfo.InvariantCulture);

        result.ShouldBe(false);
    }

    [Fact]
    public void ReturnFalseWhenBothValueAndParameterAreNull()
    {
        var result = _converter.Convert(null, typeof(bool), null, CultureInfo.InvariantCulture);

        result.ShouldBe(false);
    }

    [Fact]
    public void ReturnFalseWhenValueIsNotEnum()
    {
        var result = _converter.Convert("not an enum", typeof(bool), ThemePreference.OriginalAuto, CultureInfo.InvariantCulture);

        result.ShouldBe(false);
    }

    [Fact]
    public void ReturnFalseWhenDifferentEnumTypes()
    {
        var result = _converter.Convert(ThemePreference.OriginalAuto, typeof(bool), 42, CultureInfo.InvariantCulture);

        result.ShouldBe(false);
    }

    [Fact]
    public void ConvertBackReturnParameterWhenValueIsTrue()
    {
        var result = _converter.ConvertBack(true, typeof(ThemePreference), ThemePreference.OriginalAuto, CultureInfo.InvariantCulture);

        result.ShouldBe(ThemePreference.OriginalAuto);
    }

    [Fact]
    public void ConvertBackReturnNullWhenValueIsFalse()
    {
        var result = _converter.ConvertBack(false, typeof(ThemePreference), ThemePreference.OriginalAuto, CultureInfo.InvariantCulture);

        result.ShouldBeNull();
    }

    [Fact]
    public void ConvertBackReturnNullWhenValueIsNotBoolean()
    {
        var result = _converter.ConvertBack("not a bool", typeof(ThemePreference), ThemePreference.OriginalAuto, CultureInfo.InvariantCulture);

        result.ShouldBeNull();
    }

    [Fact]
    public void ConvertBackReturnNullWhenParameterIsNull()
    {
        var result = _converter.ConvertBack(true, typeof(ThemePreference), null, CultureInfo.InvariantCulture);

        result.ShouldBeNull();
    }

    [Fact]
    public void ConvertBackReturnNullWhenValueIsTrueButParameterIsNull()
    {
        var result = _converter.ConvertBack(true, typeof(ThemePreference), null, CultureInfo.InvariantCulture);

        result.ShouldBeNull();
    }
}
