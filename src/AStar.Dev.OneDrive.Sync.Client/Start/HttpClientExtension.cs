using System.Net;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;

namespace AStar.Dev.OneDrive.Sync.Client;

public static class HttpClientExtension
{
    public static void AddHttpClientWithRetry(this IServiceCollection services) => _ = services.AddHttpClient<IGraphApiClient, GraphApiClient>()
                        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = true, MaxConnectionsPerServer = 10 })
                        .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMinutes(5))
                        .AddPolicyHandler(GetRetryPolicy())
                        .AddPolicyHandler(GetCircuitBreakerPolicy());

    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy() => Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<IOException>(ex => ex.GetBaseException().Message.Contains("forcibly closed") || ex.GetBaseException().Message.Contains("transport connection"))
                .OrResult(msg => (int)msg.StatusCode >= 500 || msg.StatusCode == HttpStatusCode.TooManyRequests || msg.StatusCode == HttpStatusCode.RequestTimeout)
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (outcome, timespan, retryCount, context) =>
                    {
                        var error = outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown";
                        Console.WriteLine($"[Graph API] Retry {retryCount}/3 after {timespan.TotalSeconds:F1}s. Reason: {error}");
                    });

    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() => HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    5,
                    TimeSpan.FromSeconds(30),
                    (outcome, duration) => Console.WriteLine($"Circuit breaker opened for {duration.TotalSeconds}s due to {outcome.Result?.StatusCode}"),
                    () => Console.WriteLine("Circuit breaker reset"));
}
