using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Converters;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Shouldly;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Converters;

public class BoolToStatusColorConverterShould
{
    private readonly BoolToStatusColorConverter _converter;

    public BoolToStatusColorConverterShould() => _converter = new BoolToStatusColorConverter();

    [Fact]
    public void ReturnTealColorWhenValueIsTrue()
    {
        var result = _converter.Convert(true, typeof(IBrush), null, CultureInfo.InvariantCulture);

        _ = result.ShouldBeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result;
        brush.Color.ShouldBe(Color.Parse("#4ECDC4"));
    }

    [Fact]
    public void ReturnRedColorWhenValueIsFalse()
    {
        var result = _converter.Convert(false, typeof(IBrush), null, CultureInfo.InvariantCulture);

        _ = result.ShouldBeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result;
        brush.Color.ShouldBe(Color.Parse("#FF6B6B"));
    }

    [Fact]
    public void ReturnGrayColorWhenValueIsNull()
    {
        var result = _converter.Convert(null, typeof(IBrush), null, CultureInfo.InvariantCulture);

        _ = result.ShouldBeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result;
        brush.Color.ShouldBe(Color.Parse("#CCCCCC"));
    }

    [Fact]
    public void ReturnGrayColorWhenValueIsNotBoolean()
    {
        var result = _converter.Convert("not a boolean", typeof(IBrush), null, CultureInfo.InvariantCulture);

        _ = result.ShouldBeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result;
        brush.Color.ShouldBe(Color.Parse("#CCCCCC"));
    }

    [Fact]
    public void ReturnGrayColorWhenValueIsInteger()
    {
        var result = _converter.Convert(42, typeof(IBrush), null, CultureInfo.InvariantCulture);

        _ = result.ShouldBeOfType<SolidColorBrush>();
        var brush = (SolidColorBrush)result;
        brush.Color.ShouldBe(Color.Parse("#CCCCCC"));
    }

    [Fact]
    public void ThrowNotSupportedExceptionWhenConvertBackIsCalled()
        => _ = Should.Throw<NotSupportedException>(() => _converter.ConvertBack(new SolidColorBrush(Colors.Blue), typeof(bool), null, CultureInfo.InvariantCulture));
}
