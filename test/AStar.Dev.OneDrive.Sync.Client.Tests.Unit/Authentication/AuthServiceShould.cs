using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication;
using Microsoft.Identity.Client;
using AuthenticationResult = AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.Authentication.AuthenticationResult;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Authentication;

public class AuthServiceShould
{
    private static AuthConfiguration CreateTestConfiguration() => new() { ClientId = "test-client-id", RedirectUri = "http://localhost", Authority = "https://login.microsoftonline.com/common", Scopes = ["test.scope"] };

    [Fact]
    public async Task ReturnSuccessResultWhenLoginSucceeds()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "access_token");

        _ = mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(mockAuthResult);

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Result<AuthenticationResult, ErrorResponse> result = await service.LoginAsync(TestContext.Current.CancellationToken);
        AuthenticationResult authResult = result.Match(
            success => success,
            error => throw new InvalidOperationException("Expected success"));

        authResult.Success.ShouldBeTrue();
        authResult.AccountId.ShouldBe("acc1");
        authResult.HashedAccountId.Value.ShouldBe(AccountIdHasher.Hash("acc1"));
        authResult.DisplayName.ShouldBe("user@example.com");
        authResult.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnFailureResultWhenLoginThrowsMsalException()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        _ = mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<MsalAuthResult, ErrorResponse>>(new Result<MsalAuthResult, ErrorResponse>.Error(new ErrorResponse("login_failed"))));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Result<AuthenticationResult, ErrorResponse> result = await service.LoginAsync(TestContext.Current.CancellationToken);
        AuthenticationResult authResult = result.Match(
            success => success,
            error => AuthenticationResult.Failed(error.Message));

        authResult.Success.ShouldBeFalse();
        authResult.AccountId.ShouldBeEmpty();
        authResult.HashedAccountId.Value.ShouldBeEmpty();
        authResult.DisplayName.ShouldBeEmpty();
        _ = authResult.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnFailureResultWhenLoginIsCancelled()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<MsalAuthResult, ErrorResponse>>(new Result<MsalAuthResult, ErrorResponse>.Error(new ErrorResponse("cancelled"))));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Result<AuthenticationResult, ErrorResponse> result = await service.LoginAsync(cts.Token);
        AuthenticationResult authResult = result.Match(
            success => success,
            error => AuthenticationResult.Failed(error.Message));

        authResult.Success.ShouldBeFalse();
        authResult.ErrorMessage.ShouldBe("Login was cancelled.");
    }

    [Fact]
    public async Task ReturnTrueWhenLogoutSucceeds()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");

        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([mockAccount])));
        _ = mockClient.RemoveAsync(mockAccount, TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<AStar.Dev.Functional.Extensions.Unit, ErrorResponse>>(new Result<AStar.Dev.Functional.Extensions.Unit, ErrorResponse>.Ok(AStar.Dev.Functional.Extensions.Unit.Value)));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Result<bool, ErrorResponse> result = await service.LogoutAsync("acc1", TestContext.Current.CancellationToken);
        var logoutResult = result.Match(
            success => success,
            error => throw new InvalidOperationException("Expected success"));

        logoutResult.ShouldBeTrue();
        await mockClient.Received(1).RemoveAsync(mockAccount, TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task ReturnFalseWhenLogoutAccountNotFound()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();

        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([])));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Result<bool, ErrorResponse> result = await service.LogoutAsync("nonexistent", TestContext.Current.CancellationToken);
        var logoutResult = result.Match(
            success => success,
            error => false);
        logoutResult.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnEmptyListWhenNoAccountsAuthenticated()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([])));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Result<IReadOnlyList<(string accountId, string DisplayName)>, ErrorResponse> result = await service.GetAuthenticatedAccountsAsync(TestContext.Current.CancellationToken);
        IReadOnlyList<(string accountId, string DisplayName)> accounts = result.Match(
            success => success,
            error => throw new InvalidOperationException("Expected success"));

        accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReturnAuthenticatedAccountsCorrectly()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount account1 = CreateMockAccount("acc1", "user1@example.com");
        IAccount account2 = CreateMockAccount("acc2", "user2@example.com");

        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([account1, account2])));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Result<IReadOnlyList<(string accountId, string DisplayName)>, ErrorResponse> result = await service.GetAuthenticatedAccountsAsync(TestContext.Current.CancellationToken);
        IReadOnlyList<(string accountId, string DisplayName)> accounts = result.Match(
            success => success,
            error => throw new InvalidOperationException("Expected success"));
        accounts.Count.ShouldBe(2);
        accounts[0].accountId.ShouldBe("acc1");
        accounts[0].DisplayName.ShouldBe("user1@example.com");
        accounts[1].accountId.ShouldBe("acc2");
        accounts[1].DisplayName.ShouldBe("user2@example.com");
    }

    [Fact]
    public async Task ReturnAccessTokenWhenGetAccessTokenSucceeds()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "token123");

        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([mockAccount])));
        _ = mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(mockAuthResult);

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Result<string?, ErrorResponse> result = await service.GetAccessTokenAsync("acc1", TestContext.Current.CancellationToken);
        var token = result.Match(
            success => success,
            error => null);

        token.ShouldBe("token123");
    }

    [Fact]
    public async Task ReturnNullWhenGetAccessTokenAccountNotFound()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([])));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Result<string?, ErrorResponse> result = await service.GetAccessTokenAsync("nonexistent", TestContext.Current.CancellationToken);
        var token = result.Match(
            success => success,
            error => null);
        token.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnNullWhenGetAccessTokenThrowsMsalUiRequiredException()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");

        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([mockAccount])));
        _ = mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Result<MsalAuthResult, ErrorResponse>>(
                new MsalUiRequiredException("error_code", "User interaction required")));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Result<string?, ErrorResponse> result = await service.GetAccessTokenAsync("acc1", TestContext.Current.CancellationToken);
        var token = result.Match(
            success => success,
            error => null);

        token.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnFalseWhenAccountNotAuthenticated()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([])));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Result<bool, ErrorResponse> result = await service.IsAuthenticatedAsync("acc1", TestContext.Current.CancellationToken);
        var isAuthenticated = result.Match(
            success => success,
            error => false);
        isAuthenticated.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnTrueWhenAccountIsAuthenticated()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");

        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([mockAccount])));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Result<bool, ErrorResponse> result = await service.IsAuthenticatedAsync("acc1", TestContext.Current.CancellationToken);
        var isAuthenticated = result.Match(
            success => success,
            error => false);
        isAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task AcquireTokenSilentlyCallsGetAccessToken()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "token456");

        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([mockAccount])));
        _ = mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(mockAuthResult);

        var service = new AuthService(mockClient, CreateTestConfiguration());

        Result<string?, ErrorResponse> result = await service.AcquireTokenSilentAsync("acc1", TestContext.Current.CancellationToken);
        var token = result.Match(
            success => success,
            error => null);

        token.ShouldBe("token456");
    }

    private static IAccount CreateMockAccount(string accountId, string username)
    {
        IAccount account = Substitute.For<IAccount>();
        _ = account.HomeAccountId.Returns(new AccountId(accountId, accountId, accountId));
        _ = account.Username.Returns(username);
        return account;
    }
}
