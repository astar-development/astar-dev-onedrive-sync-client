namespace AStar.Dev.Functional.Extensions.Tests.Unit;

public class ConvenienceResultExtensionsShould
{
    [Fact(Skip = "Extension method not implemented yet")]
    public void ReturnTheValueFromGetOrThrowOnSuccess()
    {
        true.ShouldBeTrue();
    }

    [Fact(Skip = "Extension method not implemented yet")]
    public void ThrowTheCapturedExceptionFromGetOrThrowOnError()
    {
        true.ShouldBeTrue();
    }

    [Fact(Skip = "Extension method not implemented yet")]
    public async Task ReturnTheValueFromGetOrThrowAsyncOnSuccess()
    {
        await Task.CompletedTask;
        true.ShouldBeTrue();
    }

    [Fact(Skip = "Extension method not implemented yet")]
    public async Task ThrowTheCapturedExceptionFromGetOrThrowAsyncOnError()
    {
        await Task.CompletedTask;
        true.ShouldBeTrue();
    }

    [Fact(Skip = "Extension method not implemented yet")]
    public void ReturnEmptyStringFromToErrorMessageOnSuccess()
    {
        true.ShouldBeTrue();
    }

    [Fact(Skip = "Extension method not implemented yet")]
    public void ReturnExceptionMessageFromToErrorMessageOnError()
    {
        true.ShouldBeTrue();
    }
}
