using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Converters;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Converters;

public class BoolToStatusTextConverterShould
{
    private readonly BoolToStatusTextConverter _converter;

    public BoolToStatusTextConverterShould() => _converter = new BoolToStatusTextConverter();

    [Fact]
    public void ReturnConnectedWhenValueIsTrue()
    {
        var result = _converter.Convert(true, typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("✓ Connected");
    }

    [Fact]
    public void ReturnDisconnectedWhenValueIsFalse()
    {
        var result = _converter.Convert(false, typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("✗ Disconnected");
    }

    [Fact]
    public void ReturnUnknownWhenValueIsNull()
    {
        var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("Unknown");
    }

    [Fact]
    public void ReturnUnknownWhenValueIsNotBoolean()
    {
        var result = _converter.Convert("not a boolean", typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("Unknown");
    }

    [Fact]
    public void ReturnUnknownWhenValueIsInteger()
    {
        var result = _converter.Convert(42, typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("Unknown");
    }

    [Fact]
    public void ThrowNotSupportedExceptionWhenConvertBackIsCalled()
        => _ = Should.Throw<NotSupportedException>(() => _converter.ConvertBack("✓ Connected", typeof(bool), null, CultureInfo.InvariantCulture));
}
