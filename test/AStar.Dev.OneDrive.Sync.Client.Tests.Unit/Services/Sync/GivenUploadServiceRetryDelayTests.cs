using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public sealed class GivenUploadServiceRetryDelayTests
{
    private static MethodInfo GetRetryDelayMethod() =>
        typeof(UploadService).GetMethod("GetRetryDelay", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Fact]
    public void when_retry_after_delta_header_is_present_then_delay_is_delta_plus_one_second()
    {
        var method = GetRetryDelayMethod();
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(5));

        var result = (TimeSpan)method.Invoke(null, [response, 1])!;

        result.ShouldBe(TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void when_retry_after_date_header_is_in_the_future_then_delay_is_wait_plus_one_second()
    {
        var method = GetRetryDelayMethod();
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        var futureDate = DateTimeOffset.UtcNow.AddSeconds(10);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(futureDate);

        var result = (TimeSpan)method.Invoke(null, [response, 1])!;

        result.TotalSeconds.ShouldBeGreaterThan(9.0);
        result.TotalSeconds.ShouldBeLessThan(13.0);
    }

    [Fact]
    public void when_retry_after_date_header_is_in_the_past_then_falls_back_to_backoff_delay()
    {
        var method = GetRetryDelayMethod();
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        var pastDate = DateTimeOffset.UtcNow.AddSeconds(-10);
        response.Headers.RetryAfter = new RetryConditionHeaderValue(pastDate);

        var result = (TimeSpan)method.Invoke(null, [response, 1])!;

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(2.0);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(2.4);
    }

    [Fact]
    public void when_no_retry_after_header_is_present_then_falls_back_to_backoff_delay()
    {
        var method = GetRetryDelayMethod();
        var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);

        var result = (TimeSpan)method.Invoke(null, [response, 1])!;

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(2.0);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(2.4);
    }
}
