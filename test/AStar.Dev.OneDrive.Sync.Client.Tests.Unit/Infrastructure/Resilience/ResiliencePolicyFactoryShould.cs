using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Resilience;
using Polly;
using Polly.CircuitBreaker;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Resilience;

public class ResiliencePolicyFactoryShould
{
    private readonly ResiliencePolicyFactory _factory;

    public ResiliencePolicyFactoryShould() => _factory = new ResiliencePolicyFactory();

    [Fact]
    public void CreateHttpRetryPolicyWithExponentialBackoff()
    {
        AsyncPolicy<HttpResponseMessage> policy = _factory.CreateHttpRetryPolicy();

        policy.ShouldNotBeNull();
        policy.ShouldBeAssignableTo<AsyncPolicy<HttpResponseMessage>>();
    }

    [Fact]
    public void CreateDatabaseRetryPolicyWithExponentialBackoff()
    {
        AsyncPolicy policy = _factory.CreateDatabaseRetryPolicy();

        policy.ShouldNotBeNull();
    }

    [Fact]
    public void CreateCircuitBreakerPolicyWithConfigurableThresholds()
    {
        AsyncPolicy<HttpResponseMessage> policy = _factory.CreateCircuitBreakerPolicy<HttpResponseMessage>(
            exceptionsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30));

        policy.ShouldNotBeNull();
    }

    [Fact]
    public async Task RetryTransientFailures()
    {
        AsyncPolicy<HttpResponseMessage> policy = _factory.CreateHttpRetryPolicy();
        var attempt = 0;

        HttpResponseMessage result = await policy.ExecuteAsync(async () =>
        {
            attempt++;
            return attempt < 3
                ? new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                : new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        result.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        attempt.ShouldBe(3);
    }

    [Fact]
    public async Task OpenTheCircuitBreakerAfterConsecutiveFailures()
    {
        AsyncPolicy<int> policy = _factory.CreateCircuitBreakerPolicy<int>(
            exceptionsAllowedBeforeBreaking: 2,
            durationOfBreak: TimeSpan.FromMilliseconds(100));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            policy.ExecuteAsync(() => throw new InvalidOperationException("Test failure")));

        await Should.ThrowAsync<InvalidOperationException>(() =>
            policy.ExecuteAsync(() => throw new InvalidOperationException("Test failure")));

        await Should.ThrowAsync<BrokenCircuitException>(() =>
            policy.ExecuteAsync(() => Task.FromResult(1)));
    }
}
