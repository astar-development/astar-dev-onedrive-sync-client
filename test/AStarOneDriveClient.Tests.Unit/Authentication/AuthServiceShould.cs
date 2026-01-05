using AStarOneDriveClient.Authentication;
using Microsoft.Identity.Client;

namespace AStarOneDriveClient.Tests.Unit.Authentication;

public class AuthServiceShould
{
    [Fact]
    public void ThrowArgumentNullExceptionWhenAuthClientIsNull()
    {
        var exception = Should.Throw<ArgumentNullException>(
            () => new AuthService(null!)
        );

        exception.ParamName.ShouldBe("authClient");
    }

    [Fact]
    public async Task ReturnSuccessResultWhenLoginSucceeds()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        var mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "access_token");

        mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(mockAuthResult);

        var service = new AuthService(mockClient);

        var result = await service.LoginAsync();

        result.Success.ShouldBeTrue();
        result.AccountId.ShouldBe("acc1");
        result.DisplayName.ShouldBe("user@example.com");
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnFailureResultWhenLoginThrowsMsalException()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MsalAuthResult>(new MsalException("login_failed")));

        var service = new AuthService(mockClient);

        var result = await service.LoginAsync();

        result.Success.ShouldBeFalse();
        result.AccountId.ShouldBeNull();
        result.DisplayName.ShouldBeNull();
        result.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnFailureResultWhenLoginIsCancelled()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MsalAuthResult>(new OperationCanceledException()));

        var service = new AuthService(mockClient);

        var result = await service.LoginAsync(cts.Token);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Login was cancelled.");
    }

    [Fact]
    public async Task ReturnTrueWhenLogoutSucceeds()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        var mockAccount = CreateMockAccount("acc1", "user@example.com");

        mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>(new[] { mockAccount }));
        mockClient.RemoveAsync(mockAccount).Returns(Task.CompletedTask);

        var service = new AuthService(mockClient);

        var result = await service.LogoutAsync("acc1");

        result.ShouldBeTrue();
        await mockClient.Received(1).RemoveAsync(mockAccount);
    }

    [Fact]
    public async Task ReturnFalseWhenLogoutAccountNotFound()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();

        mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>(Array.Empty<IAccount>()));

        var service = new AuthService(mockClient);

        var result = await service.LogoutAsync("nonexistent");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnEmptyListWhenNoAccountsAuthenticated()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>(Array.Empty<IAccount>()));

        var service = new AuthService(mockClient);

        var result = await service.GetAuthenticatedAccountsAsync();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReturnAuthenticatedAccountsCorrectly()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        var account1 = CreateMockAccount("acc1", "user1@example.com");
        var account2 = CreateMockAccount("acc2", "user2@example.com");

        mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>(new[] { account1, account2 }));

        var service = new AuthService(mockClient);

        var result = await service.GetAuthenticatedAccountsAsync();

        result.Count.ShouldBe(2);
        result[0].AccountId.ShouldBe("acc1");
        result[0].DisplayName.ShouldBe("user1@example.com");
        result[1].AccountId.ShouldBe("acc2");
        result[1].DisplayName.ShouldBe("user2@example.com");
    }

    [Fact]
    public async Task ReturnAccessTokenWhenGetAccessTokenSucceeds()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        var mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "token123");

        mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>(new[] { mockAccount }));
        mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(mockAuthResult);

        var service = new AuthService(mockClient);

        var result = await service.GetAccessTokenAsync("acc1");

        result.ShouldBe("token123");
    }

    [Fact]
    public async Task ReturnNullWhenGetAccessTokenAccountNotFound()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>(Array.Empty<IAccount>()));

        var service = new AuthService(mockClient);

        var result = await service.GetAccessTokenAsync("nonexistent");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnNullWhenGetAccessTokenThrowsMsalUiRequiredException()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        var mockAccount = CreateMockAccount("acc1", "user@example.com");

        mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>(new[] { mockAccount }));
        mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MsalAuthResult>(
                new MsalUiRequiredException("error_code", "User interaction required")));

        var service = new AuthService(mockClient);

        var result = await service.GetAccessTokenAsync("acc1");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnFalseWhenAccountNotAuthenticated()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>(Array.Empty<IAccount>()));

        var service = new AuthService(mockClient);

        var result = await service.IsAuthenticatedAsync("acc1");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnTrueWhenAccountIsAuthenticated()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        var mockAccount = CreateMockAccount("acc1", "user@example.com");

        mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>(new[] { mockAccount }));

        var service = new AuthService(mockClient);

        var result = await service.IsAuthenticatedAsync("acc1");

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task AcquireTokenSilentlyCallsGetAccessToken()
    {
        IAuthenticationClient mockClient = Substitute.For<IAuthenticationClient>();
        var mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "token456");

        mockClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>(new[] { mockAccount }));
        mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(mockAuthResult);

        var service = new AuthService(mockClient);

        var result = await service.AcquireTokenSilentAsync("acc1");

        result.ShouldBe("token456");
    }

    private static IAccount CreateMockAccount(string accountId, string username)
    {
        var account = Substitute.For<IAccount>();
        account.HomeAccountId.Returns(new AccountId(accountId, accountId, accountId));
        account.Username.Returns(username);
        return account;
    }
}
