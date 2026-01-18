namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class ResultExtensionMapShould
{
    [Fact]
    public void MapSuccessValueWhenResultIsOk()
    {
        var result = new Result<int, string>.Ok(42);

        Result<string, string> mapped = result.Map(value => value.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
            ok => ok,
            err => throw new InvalidOperationException("Should not be error")
        );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public void PreserveErrorWhenMappingFailedResult()
    {
        var result = new Result<int, string>.Error("error message");

        Result<string, string> mapped = result.Map(value => value.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("error message");
    }

    [Fact]
    public async Task MapSuccessValueAsyncWhenResultIsOkAsync()
    {
        var result = new Result<int, string>.Ok(42);

        Result<string, string> mapped = await result.MapAsync(value => Task.FromResult(value.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
            ok => ok,
            _ => throw new InvalidOperationException("Should not be error")
        );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveErrorWhenMappingFailedResultAsync()
    {
        var result = new Result<int, string>.Error("error message");

        Result<string, string> mapped = await result.MapAsync(value => Task.FromResult(value.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("error message");
    }

    [Fact]
    public async Task MapSuccessValueFromTaskResultWhenResultIsOkAsync()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, string> mapped = await resultTask.MapAsync(value => value.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
            ok => ok,
            _ => throw new InvalidOperationException("Should not be error")
        );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveErrorWhenMappingFailedTaskResultAsync()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Error("error message"));

        Result<string, string> mapped = await resultTask.MapAsync(value => value.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("error message");
    }

    [Fact]
    public async Task MapSuccessValueAsyncFromTaskResultWhenResultIsOkAsync()
    {
        var resultTask = Task.FromResult<Result<int, string>>(new Result<int, string>.Ok(42));

        Result<string, string> mapped = await resultTask.MapAsync(value => Task.FromResult(value.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
            ok => ok,
            _ => throw new InvalidOperationException("Should not be error")
        );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public void MapErrorValueWhenResultIsError()
    {
        var result = new Result<string, int>.Error(42);

        Result<string, string> mapped = result.MapFailure(error => error.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public void PreserveSuccessWhenMapFailureOnSuccessResult()
    {
        var result = new Result<string, int>.Ok("success");

        Result<string, string> mapped = result.MapFailure(error => error.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
            ok => ok,
            _ => throw new InvalidOperationException("Should not be error")
        );

        matchResult.ShouldBe("success");
    }

    [Fact]
    public async Task MapErrorValueAsyncWhenResultIsErrorAsync()
    {
        var result = new Result<string, int>.Error(42);

        Result<string, string> mapped = await result.MapFailureAsync(error => Task.FromResult(error.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveSuccessWhenMapFailureAsyncOnSuccessResult()
    {
        var result = new Result<string, int>.Ok("success");

        Result<string, string> mapped = await result.MapFailureAsync(error => Task.FromResult(error.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
            ok => ok,
            _ => throw new InvalidOperationException("Should not be error")
        );

        matchResult.ShouldBe("success");
    }

    [Fact]
    public async Task MapErrorValueFromTaskResultWhenResultIsErrorAsync()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));

        Result<string, string> mapped = await resultTask.MapFailureAsync(error => error.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveSuccessWhenMapFailureOnSuccessTaskResult()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));

        Result<string, string> mapped = await resultTask.MapFailureAsync(error => error.ToString());

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
            ok => ok,
            _ => throw new InvalidOperationException("Should not be error")
        );

        matchResult.ShouldBe("success");
    }

    [Fact]
    public async Task MapErrorValueAsyncFromTaskResultWhenResultIsErrorAsync()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Error(42));

        Result<string, string> mapped = await resultTask.MapFailureAsync(error => Task.FromResult(error.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Error>();

        var matchResult = mapped.Match(
            _ => throw new InvalidOperationException("Should not be success"),
            err => err
        );

        matchResult.ShouldBe("42");
    }

    [Fact]
    public async Task PreserveSuccessWhenMapFailureAsyncOnSuccessTaskResult()
    {
        var resultTask = Task.FromResult<Result<string, int>>(new Result<string, int>.Ok("success"));

        Result<string, string> mapped = await resultTask.MapFailureAsync(error => Task.FromResult(error.ToString()));

        _ = mapped.ShouldBeOfType<Result<string, string>.Ok>();

        var matchResult = mapped.Match(
            ok => ok,
            _ => throw new InvalidOperationException("Should not be error")
        );

        matchResult.ShouldBe("success");
    }
}
