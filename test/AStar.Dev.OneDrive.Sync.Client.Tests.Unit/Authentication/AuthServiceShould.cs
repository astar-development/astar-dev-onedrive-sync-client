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
            .Returns(Task.FromResult<Result<MsalAuthResult, ErrorResponse>>(mockAuthResult));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.LoginAsync(TestContext.Current.CancellationToken);

        result.Match<global::AStar.Dev.Functional.Extensions.Unit>(
            success =>
            {
                success.Success.ShouldBeTrue();
                success.AccountId.ShouldBe("acc1");
                success.HashedAccountId.Value.ShouldBe(AccountIdHasher.Hash("acc1"));
                success.DisplayName.ShouldBe("user@example.com");
                success.ErrorMessage.ShouldBeNull();
                return global::AStar.Dev.Functional.Extensions.Unit.Value;
            },
            error => throw new InvalidOperationException(error.Message));
    }

    [Fact]
    public async Task ReturnFailureResultWhenLoginThrowsMsalException()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        _ = mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<MsalAuthResult, ErrorResponse>>(new ErrorResponse("Login failed.")));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.LoginAsync(TestContext.Current.CancellationToken);

        result.Match<global::AStar.Dev.Functional.Extensions.Unit>(
            success => throw new InvalidOperationException("Should have failed"),
            error =>
            {
                _ = error.Message.ShouldNotBeNull();
                return global::AStar.Dev.Functional.Extensions.Unit.Value;
            });
    }

    [Fact]
    public async Task ReturnFailureResultWhenLoginIsCancelled()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        _ = mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<MsalAuthResult, ErrorResponse>>(new ErrorResponse("Login was cancelled.")));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.LoginAsync(cts.Token);

        result.Match<global::AStar.Dev.Functional.Extensions.Unit>(
            success => throw new InvalidOperationException("Should have failed"),
            error =>
            {
                error.Message.ShouldBe("Login was cancelled.");
                return global::AStar.Dev.Functional.Extensions.Unit.Value;
            });
    }

    [Fact]
    public async Task ReturnTrueWhenLogoutSucceeds()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");

        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([mockAccount])));
        _ = mockClient.RemoveAsync(mockAccount, TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<global::AStar.Dev.Functional.Extensions.Unit, ErrorResponse>>(new Result<global::AStar.Dev.Functional.Extensions.Unit, ErrorResponse>.Ok(global::AStar.Dev.Functional.Extensions.Unit.Value)));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.LogoutAsync("acc1", TestContext.Current.CancellationToken);

        result.Match<global::AStar.Dev.Functional.Extensions.Unit>(v =>
        {
            v.ShouldBeTrue();
            return global::AStar.Dev.Functional.Extensions.Unit.Value;
        }, e => throw new InvalidOperationException(e.Message));
        await mockClient.Received(1).RemoveAsync(mockAccount, TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "Runs on it's own but not when run with other tests - or is flaky and works sometimes when run with others")]
    public async Task ReturnFalseWhenLogoutAccountNotFound()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();

        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([])));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.LogoutAsync("nonexistent", TestContext.Current.CancellationToken);
        result.Match<global::AStar.Dev.Functional.Extensions.Unit>(v =>
        {
            v.ShouldBeFalse();
            return global::AStar.Dev.Functional.Extensions.Unit.Value;
        }, e => throw new InvalidOperationException(e.Message));
    }

    [Fact]
    public async Task ReturnEmptyListWhenNoAccountsAuthenticated()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([])));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.GetAuthenticatedAccountsAsync(TestContext.Current.CancellationToken);

        result.Match<global::AStar.Dev.Functional.Extensions.Unit>(v =>
        {
            v.ShouldBeEmpty();
            return global::AStar.Dev.Functional.Extensions.Unit.Value;
        }, e => throw new InvalidOperationException(e.Message));
    }

    [Fact]
    public async Task ReturnAuthenticatedAccountsCorrectly()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount account1 = CreateMockAccount("acc1", "user1@example.com");
        IAccount account2 = CreateMockAccount("acc2", "user2@example.com");

        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([account1, account2])));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.GetAuthenticatedAccountsAsync(TestContext.Current.CancellationToken);
        result.Match<global::AStar.Dev.Functional.Extensions.Unit>(v =>
        {
            v.Count.ShouldBe(2);
            v[0].accountId.ShouldBe("acc1");
            v[0].DisplayName.ShouldBe("user1@example.com");
            v[1].accountId.ShouldBe("acc2");
            v[1].DisplayName.ShouldBe("user2@example.com");
            return global::AStar.Dev.Functional.Extensions.Unit.Value;
        }, e => throw new InvalidOperationException(e.Message));
    }

    [Fact]
    public async Task ReturnAccessTokenWhenGetAccessTokenSucceeds()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "token123");

        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([mockAccount])));
        _ = mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<MsalAuthResult, ErrorResponse>>(mockAuthResult));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.GetAccessTokenAsync("acc1", TestContext.Current.CancellationToken);

        result.Match<global::AStar.Dev.Functional.Extensions.Unit>(v =>
        {
            v.ShouldBe("token123");
            return global::AStar.Dev.Functional.Extensions.Unit.Value;
        }, e => throw new InvalidOperationException(e.Message));
    }

    [Fact]
    public async Task ReturnNullWhenGetAccessTokenAccountNotFound()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([])));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.GetAccessTokenAsync("nonexistent", TestContext.Current.CancellationToken);
        result.Match<global::AStar.Dev.Functional.Extensions.Unit>(v =>
        {
            v.ShouldBeNull();
            return global::AStar.Dev.Functional.Extensions.Unit.Value;
        }, e => throw new InvalidOperationException(e.Message));
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

        var result = await service.GetAccessTokenAsync("acc1", TestContext.Current.CancellationToken);

        result.Match<global::AStar.Dev.Functional.Extensions.Unit>(v =>
        {
            v.ShouldBeNull();
            return global::AStar.Dev.Functional.Extensions.Unit.Value;
        }, e => throw new InvalidOperationException(e.Message));
    }

    [Fact]
    public async Task ReturnFalseWhenAccountNotAuthenticated()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([])));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.IsAuthenticatedAsync("acc1", TestContext.Current.CancellationToken);
        result.Match<global::AStar.Dev.Functional.Extensions.Unit>(v =>
        {
            v.ShouldBeFalse();
            return global::AStar.Dev.Functional.Extensions.Unit.Value;
        }, e => throw new InvalidOperationException(e.Message));
    }

    [Fact]
    public async Task ReturnTrueWhenAccountIsAuthenticated()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");

        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([mockAccount])));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.IsAuthenticatedAsync("acc1", TestContext.Current.CancellationToken);
        result.Match<global::AStar.Dev.Functional.Extensions.Unit>(v =>
        {
            v.ShouldBeTrue();
            return global::AStar.Dev.Functional.Extensions.Unit.Value;
        }, e => throw new InvalidOperationException(e.Message));
    }

    [Fact]
    public async Task AcquireTokenSilentlyCallsGetAccessToken()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        IAccount mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "token456");

        _ = mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<Result<IEnumerable<IAccount>, ErrorResponse>>(new Result<IEnumerable<IAccount>, ErrorResponse>.Ok([mockAccount])));
        _ = mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Result<MsalAuthResult, ErrorResponse>>(mockAuthResult));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.AcquireTokenSilentAsync("acc1", TestContext.Current.CancellationToken);

        result.Match<global::AStar.Dev.Functional.Extensions.Unit>(v =>
        {
            v.ShouldBe("token456");
            return global::AStar.Dev.Functional.Extensions.Unit.Value;
        }, e => throw new InvalidOperationException(e.Message));
    }

    private static IAccount CreateMockAccount(string accountId, string username)
    {
        IAccount account = Substitute.For<IAccount>();
        _ = account.HomeAccountId.Returns(new AccountId(accountId, accountId, accountId));
        _ = account.Username.Returns(username);
        return account;
    }
}
