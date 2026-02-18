using System.Globalization;
using AStar.Dev.OneDrive.Sync.Client.Converters;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Converters;

public class InitialsConverterShould
{
    private readonly InitialsConverter _converter;

    public InitialsConverterShould() => _converter = new InitialsConverter();

    [Fact]
    public void ReturnQuestionMarkWhenValueIsNull()
    {
        var result = _converter.Convert(null, typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("?");
    }

    [Fact]
    public void ReturnQuestionMarkWhenValueIsNotString()
    {
        var result = _converter.Convert(123, typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("?");
    }

    [Fact]
    public void ReturnQuestionMarkWhenValueIsEmptyString()
    {
        var result = _converter.Convert("", typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("?");
    }

    [Fact]
    public void ReturnQuestionMarkWhenValueIsWhitespace()
    {
        var result = _converter.Convert("   ", typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("?");
    }

    [Fact]
    public void ReturnFirstCharacterUppercaseWhenSingleWord()
    {
        var result = _converter.Convert("John", typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("J");
    }

    [Fact]
    public void ReturnFirstAndLastInitialsWhenTwoWords()
    {
        var result = _converter.Convert("John Doe", typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("JD");
    }

    [Fact]
    public void ReturnFirstAndLastInitialsWhenThreeWords()
    {
        var result = _converter.Convert("John Michael Doe", typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("JD");
    }

    [Fact]
    public void ReturnUppercaseInitialsRegardlessOfInputCase()
    {
        var result = _converter.Convert("john doe", typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("JD");
    }

    [Fact]
    public void HandleMultipleSpacesBetweenWords()
    {
        var result = _converter.Convert("John    Doe", typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("JD");
    }

    [Fact]
    public void HandleLeadingAndTrailingSpaces()
    {
        var result = _converter.Convert("  John Doe  ", typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("JD");
    }

    [Fact]
    public void HandleSingleCharacterName()
    {
        var result = _converter.Convert("J", typeof(string), null, CultureInfo.InvariantCulture);

        result.ShouldBe("J");
    }

    [Fact]
    public void ThrowNotSupportedExceptionWhenConvertBackIsCalled()
        => _ = Should.Throw<NotSupportedException>(() => _converter.ConvertBack("JD", typeof(string), null, CultureInfo.InvariantCulture));
}
