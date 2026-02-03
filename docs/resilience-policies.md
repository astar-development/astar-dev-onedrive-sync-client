# Polly Resilience Policies

This document describes the standard retry policies implemented using Polly for handling transient failures in the OneDrive Sync Client.

## Overview

The `ResiliencePolicyFactory` provides pre-configured resilience policies for:

- HTTP requests with exponential backoff
- Database operations with exponential backoff
- Circuit breaker patterns
- Combined resilience strategies

## Usage Examples

### HTTP Retry Policy

The HTTP retry policy automatically retries transient HTTP failures (5xx, 408, 429) with exponential backoff.

```csharp
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Resilience;

var factory = new ResiliencePolicyFactory();
var policy = factory.CreateHttpRetryPolicy(
    maxRetryAttempts: 3,
    initialDelaySeconds: 1);

// Use the policy
var httpClient = new HttpClient();
var response = await policy.ExecuteAsync(() => 
    httpClient.GetAsync("https://graph.microsoft.com/v1.0/me/drive"));
```

**Default Behavior:**

- Retries: 3 attempts
- Backoff: Exponential (1s, 2s, 4s)
- Retries on: HTTP 5xx, 408 (Request Timeout), 429 (Too Many Requests)

### Database Retry Policy

The database retry policy handles transient database errors with exponential backoff.

```csharp
var policy = factory.CreateDatabaseRetryPolicy(
    maxRetryAttempts: 3,
    initialDelaySeconds: 1);

// Use with EF Core
await policy.ExecuteAsync(async () =>
{
    await dbContext.SaveChangesAsync();
});
```

**Default Behavior:**

- Retries: 3 attempts
- Backoff: Exponential (1s, 2s, 4s)
- Retries on: TimeoutException, timeout-related errors

### Circuit Breaker Policy

The circuit breaker prevents cascading failures by "opening the circuit" after consecutive failures.

```csharp
var policy = factory.CreateCircuitBreakerPolicy<HttpResponseMessage>(
    exceptionsAllowedBeforeBreaking: 3,
    durationOfBreak: TimeSpan.FromSeconds(30));

// Circuit opens after 3 consecutive failures
// Remains open for 30 seconds before allowing retry
```

**States:**

- **Closed**: Normal operation
- **Open**: After threshold failures, blocks all requests
- **Half-Open**: After duration, allows one test request

### Combined Resilience Policy

Wrap multiple policies for comprehensive resilience.

```csharp
var policy = factory.CreateHttpResiliencePolicy(
    maxRetryAttempts: 3,
    exceptionsAllowedBeforeBreaking: 5,
    circuitBreakerDuration: TimeSpan.FromSeconds(60));

// This combines retry + circuit breaker
var response = await policy.ExecuteAsync(() => 
    httpClient.GetAsync(url));
```

## Exponential Backoff Formula

The retry delay follows exponential backoff:

```text
delay = initialDelay * 2^retryAttempt
```

**Example with initialDelay = 1 second:**

- 1st retry: 2s delay
- 2nd retry: 4s delay
- 3rd retry: 8s delay

## Guidelines

### When to Use Retry Policies

✅ **Use for:**

- HTTP requests to external APIs (Microsoft Graph, etc.)
- Database operations
- File I/O operations
- Network-dependent operations

❌ **Do NOT use for:**

- User authentication failures (not transient)
- Validation errors (immediate, not transient)
- Operations with side effects that shouldn't be repeated

### Configuring Retry Attempts

- **Critical operations**: 5-7 retries with longer backoff
- **Standard operations**: 3 retries (default)
- **Background jobs**: 10+ retries with exponential backoff

### Circuit Breaker Thresholds

- **Public APIs**: 5-10 failures before breaking
- **Internal services**: 3-5 failures before breaking
- **Break duration**: 30-60 seconds (allows time for service recovery)

## Integration with Dependency Injection

Register policies as singletons in Program.cs:

```csharp
services.AddSingleton<ResiliencePolicyFactory>();

// Or register specific policies
services.AddSingleton(sp =>
{
    var factory = new ResiliencePolicyFactory();
    return factory.CreateHttpRetryPolicy();
});
```

## Testing Resilience Policies

See `ResiliencePolicyFactoryShould.cs` for comprehensive test examples including:

- Verifying retry behavior
- Testing circuit breaker state transitions
- Validating exponential backoff timing

## Future Enhancements

- [ ] Add Polly context for passing data between retries
- [ ] Integrate with Serilog for retry/circuit breaker logging
- [ ] Add timeout policies
- [ ] Add fallback policies
- [ ] Configuration-driven policy settings (from appsettings.json)

---

**Document Version**: 1.0
**Last Updated**: February 3, 2026
**Related Documents**: [Architecture](architecture.md) | [Implementation Roadmap](roadmap.md)
