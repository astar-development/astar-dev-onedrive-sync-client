using Polly;
using Polly.CircuitBreaker;
using System.Net;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Resilience;

/// <summary>
/// Factory for creating resilience policies (retry, circuit breaker, timeout, fallback) using Polly.
/// </summary>
public class ResiliencePolicyFactory
{
    /// <summary>
    /// Creates a retry policy for HTTP requests with exponential backoff.
    /// Retries on transient HTTP errors (5xx, 408, 429).
    /// </summary>
    public AsyncPolicy<HttpResponseMessage> CreateHttpRetryPolicy(
        int maxRetryAttempts = 3,
        int initialDelaySeconds = 1)
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r =>
                r.StatusCode >= HttpStatusCode.InternalServerError || // 5xx
                r.StatusCode == HttpStatusCode.RequestTimeout ||      // 408
                r.StatusCode == (HttpStatusCode)429)                   // Too Many Requests
            .WaitAndRetryAsync(
                retryCount: maxRetryAttempts,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * initialDelaySeconds),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // TODO: Add logging
                });
    }

    /// <summary>
    /// Creates a retry policy for database operations with exponential backoff.
    /// Retries on transient database errors.
    /// </summary>
    public AsyncPolicy CreateDatabaseRetryPolicy(
        int maxRetryAttempts = 3,
        int initialDelaySeconds = 1)
    {
        return Policy
            .Handle<Exception>(ex =>
                ex is TimeoutException ||
                ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("transient", StringComparison.OrdinalIgnoreCase))
            .WaitAndRetryAsync(
                retryCount: maxRetryAttempts,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt) * initialDelaySeconds),
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    // TODO: Add logging
                });
    }

    /// <summary>
    /// Creates a circuit breaker policy with configurable thresholds.
    /// Opens the circuit after consecutive failures, preventing further requests.
    /// </summary>
    public AsyncPolicy<T> CreateCircuitBreakerPolicy<T>(
        int exceptionsAllowedBeforeBreaking = 3,
        TimeSpan? durationOfBreak = null)
    {
        var breakDuration = durationOfBreak ?? TimeSpan.FromSeconds(30);

        return Policy<T>
            .Handle<Exception>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: exceptionsAllowedBeforeBreaking,
                durationOfBreak: breakDuration,
                onBreak: (outcome, duration) =>
                {
                    // TODO: Add logging
                },
                onReset: () =>
                {
                    // TODO: Add logging
                });
    }

    /// <summary>
    /// Creates a timeout policy.
    /// </summary>
    public AsyncPolicy CreateTimeoutPolicy(TimeSpan timeout)
    {
        return Policy.TimeoutAsync(timeout);
    }

    /// <summary>
    /// Creates a combined policy (retry + circuit breaker) for HTTP requests.
    /// </summary>
    public AsyncPolicy<HttpResponseMessage> CreateHttpResiliencePolicy(
        int maxRetryAttempts = 3,
        int exceptionsAllowedBeforeBreaking = 5,
        TimeSpan? circuitBreakerDuration = null)
    {
        var retryPolicy = CreateHttpRetryPolicy(maxRetryAttempts);
        var circuitBreakerPolicy = CreateCircuitBreakerPolicy<HttpResponseMessage>(
            exceptionsAllowedBeforeBreaking,
            circuitBreakerDuration);

        return Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
    }
}
