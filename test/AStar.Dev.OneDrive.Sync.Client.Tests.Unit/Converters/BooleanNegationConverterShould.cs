using AStar.Dev.OneDrive.Sync.Client.Converters;
using Avalonia.Data.Converters;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Converters;

public class BooleanNegationConverterShould
{
    private readonly IValueConverter _sut = new BooleanNegationConverter();

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void Convert_ReturnNegationOfBoolean(bool input, bool expected)
    {
        var result = _sut.Convert(input, typeof(bool), null, System.Globalization.CultureInfo.CurrentCulture);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ConvertBack_ReturnNegationOfBoolean(bool input, bool expected)
    {
        var result = _sut.ConvertBack(input, typeof(bool), null, System.Globalization.CultureInfo.CurrentCulture);

        result.ShouldBe(expected);
    }

    [Fact]
    public void Convert_ReturnOriginalValueForNonBoolean()
    {
        var result = _sut.Convert("not a boolean", typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);

        result.ShouldBe("not a boolean");
    }
}
