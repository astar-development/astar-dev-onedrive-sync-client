using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Resilience;

public class ResiliencePolicyFactoryShould
{
    private readonly ResiliencePolicyFactory _factory;

    public ResiliencePolicyFactoryShould()
    {
        _factory = new ResiliencePolicyFactory();
    }

    [Fact]
    public void CreateHttpRetryPolicy_WithExponentialBackoff()
    {
        // Arrange & Act
        var policy = _factory.CreateHttpRetryPolicy();

        // Assert
        policy.ShouldNotBeNull();
        policy.ShouldBeAssignableTo<AsyncPolicy<HttpResponseMessage>>();
    }

    [Fact]
    public void CreateDatabaseRetryPolicy_WithExponentialBackoff()
    {
        // Arrange & Act
        var policy = _factory.CreateDatabaseRetryPolicy();

        // Assert
        policy.ShouldNotBeNull();
    }

    [Fact]
    public void CreateCircuitBreakerPolicy_WithConfigurableThresholds()
    {
        // Arrange & Act
        var policy = _factory.CreateCircuitBreakerPolicy<HttpResponseMessage>(
            exceptionsAllowedBeforeBreaking: 3,
            durationOfBreak: TimeSpan.FromSeconds(30));

        // Assert
        policy.ShouldNotBeNull();
    }

    [Fact]
    public async Task HttpRetryPolicy_ShouldRetryTransientFailures()
    {
        // Arrange
        var policy = _factory.CreateHttpRetryPolicy();
        var attempt = 0;

        // Act
        var result = await policy.ExecuteAsync(async () =>
        {
            attempt++;
            if (attempt < 3)
            {
                return new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
        });

        // Assert
        result.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        attempt.ShouldBe(3);
    }

    [Fact]
    public async Task CircuitBreakerPolicy_ShouldOpenAfterConsecutiveFailures()
    {
        // Arrange
        var policy = _factory.CreateCircuitBreakerPolicy<int>(
            exceptionsAllowedBeforeBreaking: 2,
            durationOfBreak: TimeSpan.FromMilliseconds(100));

        // Act & Assert
        // First two failures should be allowed
        await Should.ThrowAsync<InvalidOperationException>(() =>
            policy.ExecuteAsync(() => throw new InvalidOperationException("Test failure")));
        
        await Should.ThrowAsync<InvalidOperationException>(() =>
            policy.ExecuteAsync(() => throw new InvalidOperationException("Test failure")));

        // Third attempt should throw BrokenCircuitException
        await Should.ThrowAsync<BrokenCircuitException>(() =>
            policy.ExecuteAsync(() => Task.FromResult(1)));
    }
}
