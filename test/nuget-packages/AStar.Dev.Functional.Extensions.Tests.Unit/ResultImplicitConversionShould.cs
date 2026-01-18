namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class ResultImplicitConversionShould
{
    [Fact]
    public void Implicitly_convert_success_value_to_Result_ok()
    {
        // Arrange
        var value = 42;

        // Act
        Result<int, string> result = value;

        // Assert
        var (IsOk, Value, Error) = result.Match(
            ok => (IsOk: true, Value: ok, Error: null!),
            err => (IsOk: false, Value: default(int), Error: err));

        IsOk.ShouldBeTrue();
        Value.ShouldBe(42);
        Error.ShouldBeNull();
    }

    [Fact]
    public void Implicitly_convert_error_value_to_Result_error()
    {
        // Arrange
        var error = "boom";

        // Act
        Result<int, string> result = error;

        // Assert
        var (IsOk, Value, Error) = result.Match(
            ok => (IsOk: true, Value: ok, Error: null!),
            err => (IsOk: false, Value: default(int), Error: err));

        IsOk.ShouldBeFalse();
        Value.ShouldBe(default);
        Error.ShouldBe("boom");
    }
}
