using Microsoft.Identity.Client;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Auth;

/// <summary>
/// MSAL-backed authentication service for OneDrive personal accounts.
///
/// Uses the system browser + loopback redirect (http://localhost) which
/// works on both Linux and Windows without requiring WebView2.
///
/// Scopes requested:
///   Files.ReadWrite     — read/write files in OneDrive
///   offline_access      — get refresh tokens so the app works without re-auth
///   User.Read           — get display name and email from the profile
/// </summary>
public sealed class AuthService(TokenCacheService cacheService) : IAuthService
{
    // Replace with your real Azure App Registration client ID
    private const string ClientId = "3057f494-687d-4abb-a653-4b8066230b6e";

    // Personal Microsoft accounts only — do not change this
    private const string Authority = "https://login.microsoftonline.com/consumers";

    private static readonly string[] Scopes =
    [
        "Files.ReadWrite",
        "offline_access",
        "User.Read"
    ];

    private readonly IPublicClientApplication _app = PublicClientApplicationBuilder
            .Create(ClientId)
            .WithAuthority(Authority)
            .WithRedirectUri("http://localhost")
            .WithClientName("AStar.Dev.OneDrive.Sync")
            .WithClientVersion("1.0.0")
            .Build();
    private readonly TokenCacheService _cacheService = cacheService;
    private          bool                     _cacheRegistered;

    // ── IAuthService ──────────────────────────────────────────────────────

    public async Task<AuthResult> SignInInteractiveAsync(CancellationToken ct = default)
    {
        await EnsureCacheRegisteredAsync();

        try
        {
            AuthenticationResult result = await _app
                    .AcquireTokenInteractive(Scopes)
                    .WithPrompt(Prompt.SelectAccount)
                    .ExecuteAsync(ct);

            return BuildSuccess(result);
        }
        catch(MsalClientException ex) when
                (ex.ErrorCode is MsalError.AuthenticationCanceledError
                            or "authentication_canceled"
                            or "user_canceled")
        {
            return AuthResult.Cancelled();
        }
        catch(OperationCanceledException)
        {
            return AuthResult.Cancelled();
        }
        catch(MsalException ex)
        {
            return AuthResult.Failure($"Authentication failed: {ex.Message}");
        }
        catch(Exception ex)
        {
            return AuthResult.Failure($"Unexpected error during sign-in: {ex.Message}");
        }
    }

    public async Task<AuthResult> AcquireTokenSilentAsync(string accountId, CancellationToken ct = default)
    {
        await EnsureCacheRegisteredAsync();

        try
        {
            IEnumerable<IAccount> accounts = await _app.GetAccountsAsync();
            IAccount? account  = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);

            if(account is null)
                return AuthResult.Failure("Account not found in token cache.");

            AuthenticationResult result = await _app
                .AcquireTokenSilent(Scopes, account)
                .ExecuteAsync(ct);

            return BuildSuccess(result);
        }
        catch(MsalUiRequiredException)
        {
            // Silent refresh not possible — interactive sign-in required
            return AuthResult.Failure("Re-authentication required.");
        }
        catch(OperationCanceledException)
        {
            return AuthResult.Cancelled();
        }
        catch(MsalException ex)
        {
            return AuthResult.Failure($"Token refresh failed: {ex.Message}");
        }
    }

    public async Task SignOutAsync(string accountId, CancellationToken ct = default)
    {
        await EnsureCacheRegisteredAsync();

        IEnumerable<IAccount> accounts = await _app.GetAccountsAsync();
        IAccount? account  = accounts.FirstOrDefault(a => a.HomeAccountId.Identifier == accountId);

        if(account is not null)
            await _app.RemoveAsync(account);
    }

    public async Task<IReadOnlyList<string>> GetCachedAccountIdsAsync()
    {
        await EnsureCacheRegisteredAsync();

        IEnumerable<IAccount> accounts = await _app.GetAccountsAsync();
        return accounts
            .Select(a => a.HomeAccountId.Identifier)
            .ToList();
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task EnsureCacheRegisteredAsync()
    {
        if(_cacheRegistered)
            return;
        await _cacheService.RegisterAsync(_app);
        _cacheRegistered = true;
    }

    private static AuthResult BuildSuccess(AuthenticationResult result)
    {
        // Extract display name and email from the account or ID token claims
        var displayName = result.Account.Username;
        var email       = result.Account.Username;

        // Try to get a friendlier display name from the ID token claims
        if(result.ClaimsPrincipal is not null)
        {
            var nameClaim  = result.ClaimsPrincipal.FindFirst("name")?.Value;
            var emailClaim = result.ClaimsPrincipal.FindFirst("preferred_username")?.Value
                          ?? result.ClaimsPrincipal.FindFirst("email")?.Value;

            if(!string.IsNullOrEmpty(nameClaim))
                displayName = nameClaim;
            if(!string.IsNullOrEmpty(emailClaim))
                email = emailClaim;
        }

        return AuthResult.Success(
            accessToken: result.AccessToken,
            accountId: result.Account.HomeAccountId.Identifier,
            displayName: displayName,
            email: email);
    }
}
