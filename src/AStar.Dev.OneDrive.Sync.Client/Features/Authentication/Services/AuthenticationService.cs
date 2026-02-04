namespace AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;

using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

/// <summary>
/// Implements OAuth authentication with Microsoft using Device Code Flow.
/// Handles token acquisition, expiry detection, and refresh with exponential backoff.
/// </summary>
public class AuthenticationService : IAuthenticationService
{
    private const int AuthenticationTimeoutSeconds = 30;
    private const string GraphScope = "https://graph.microsoft.com/.default";
    private readonly IPublicClientApplication _publicClientApp;
    private readonly ISecureTokenStorage _secureTokenStorage;
    private readonly ILogger<AuthenticationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationService"/> class.
    /// </summary>
    /// <param name="publicClientApp">The MSAL public client application.</param>
    /// <param name="secureTokenStorage">The secure token storage service.</param>
    /// <param name="logger">The logger instance.</param>
    public AuthenticationService(
        IPublicClientApplication publicClientApp,
        ISecureTokenStorage secureTokenStorage,
        ILogger<AuthenticationService> logger)
    {
        _publicClientApp = publicClientApp ?? throw new ArgumentNullException(nameof(publicClientApp));
        _secureTokenStorage = secureTokenStorage ?? throw new ArgumentNullException(nameof(secureTokenStorage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<Result<AuthToken, AuthenticationError>> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(AuthenticationTimeoutSeconds));

            var builder = _publicClientApp.AcquireTokenWithDeviceCode(
                new[] { GraphScope },
                DisplayDeviceCodeAsync);

            var authResult = await builder.ExecuteAsync(cts.Token);

            _logger.LogInformation("User authenticated successfully via Device Code Flow");

            return new Result<AuthToken, AuthenticationError>.Ok(
                new AuthToken(authResult.AccessToken, authResult.ExpiresOn.UtcDateTime));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Authentication cancelled by user");
            return new Result<AuthToken, AuthenticationError>.Error(
                new AuthenticationError.Cancelled());
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Authentication operation timed out after {TimeoutSeconds} seconds", AuthenticationTimeoutSeconds);
            return new Result<AuthToken, AuthenticationError>.Error(
                new AuthenticationError.TimedOut());
        }
        catch (MsalServiceException ex)
        {
            _logger.LogError(ex, "MSAL service error: {ErrorCode}", ex.ErrorCode);
            return new Result<AuthToken, AuthenticationError>.Error(
                new AuthenticationError.ServiceError(ex.ErrorCode, ex.Message));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during authentication");
            return new Result<AuthToken, AuthenticationError>.Error(
                new AuthenticationError.NetworkError(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during authentication");
            return new Result<AuthToken, AuthenticationError>.Error(
                new AuthenticationError.UnexpectedError(ex.Message));
        }
    }

    /// <inheritdoc/>
    public async Task<Result<AuthToken, AuthenticationError>> RefreshTokenAsync(
        string accountIdentifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(accountIdentifier, nameof(accountIdentifier));

        const int maxRetries = 3;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var accounts = await _publicClientApp.GetAccountsAsync();
                var account = accounts.FirstOrDefault();

                if (account is null)
                {
                    return new Result<AuthToken, AuthenticationError>.Error(
                        new AuthenticationError.ConfigurationError("No cached account found for silent authentication"));
                }

                var builder = _publicClientApp.AcquireTokenSilent(
                    new[] { GraphScope },
                    account);

                var authResult = await builder.ExecuteAsync(cancellationToken);

                _logger.LogInformation("Token refreshed successfully");

                return new Result<AuthToken, AuthenticationError>.Ok(
                    new AuthToken(authResult.AccessToken, authResult.ExpiresOn.UtcDateTime));
            }
            catch (MsalServiceException ex) when (IsTransientError(ex) && attempt < maxRetries - 1)
            {
                var delayMs = CalculateBackoffDelay(attempt);
                _logger.LogWarning("Token refresh failed (transient). Attempt {Attempt}/{MaxRetries}. Retrying in {DelayMs}ms", 
                    attempt + 1, maxRetries, delayMs);

                await Task.Delay(delayMs, cancellationToken);
                continue;
            }
            catch (MsalServiceException ex)
            {
                _logger.LogError(ex, "MSAL service error during token refresh: {ErrorCode}", ex.ErrorCode);
                return new Result<AuthToken, AuthenticationError>.Error(
                    new AuthenticationError.ServiceError(ex.ErrorCode, ex.Message));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error during token refresh");
                return new Result<AuthToken, AuthenticationError>.Error(
                    new AuthenticationError.NetworkError(ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during token refresh");
                return new Result<AuthToken, AuthenticationError>.Error(
                    new AuthenticationError.UnexpectedError(ex.Message));
            }
        }

        return new Result<AuthToken, AuthenticationError>.Error(
            new AuthenticationError.UnexpectedError("Token refresh failed after maximum retries"));
    }

    private static Task DisplayDeviceCodeAsync(DeviceCodeResult deviceCodeResult)
    {
        // This would be handled by the ViewModel in the UI layer
        // The ViewModel would display the user code and verification URL
        return Task.CompletedTask;
    }

    private static bool IsTransientError(MsalServiceException ex) =>
        ex.ErrorCode is "temporarily_unavailable" or "service_not_available" or "unauthorized_client";

    private static int CalculateBackoffDelay(int attemptNumber)
    {
        const int baseDelayMs = 1000;
        var exponentialDelay = baseDelayMs * (int)Math.Pow(2, attemptNumber);
        var jitterMs = Random.Shared.Next(0, (int)(exponentialDelay * 0.1));

        return exponentialDelay + jitterMs;
    }
}
