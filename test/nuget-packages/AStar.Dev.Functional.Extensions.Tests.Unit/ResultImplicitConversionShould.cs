using System;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using Shouldly;
using Xunit;

namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class ResultImplicitConversionShould
{
    [Fact]
    public void Implicitly_convert_success_value_to_Result_ok()
    {
        // Arrange
        int value = 42;

        // Act
        Result<int, string> result = value;

        // Assert
        var matched = result.Match(
            ok => (IsOk: true, Value: ok, Error: (string?)null),
            err => (IsOk: false, Value: default(int), Error: err));

        matched.IsOk.ShouldBeTrue();
        matched.Value.ShouldBe(42);
        matched.Error.ShouldBeNull();
    }

    [Fact]
    public void Implicitly_convert_error_value_to_Result_error()
    {
        // Arrange
        string error = "boom";

        // Act
        Result<int, string> result = error;

        // Assert
        var matched = result.Match(
            ok => (IsOk: true, Value: ok, Error: (string?)null),
            err => (IsOk: false, Value: default(int), Error: err));

        matched.IsOk.ShouldBeFalse();
        matched.Value.ShouldBe(default(int));
        matched.Error.ShouldBe("boom");
    }
}
