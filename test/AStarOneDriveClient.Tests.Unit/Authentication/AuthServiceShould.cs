using AStarOneDriveClient.Authentication;
using Microsoft.Identity.Client;

namespace AStarOneDriveClient.Tests.Unit.Authentication;

public class AuthServiceShould
{
    private static AuthConfiguration CreateTestConfiguration() =>
        new()
        {
            ClientId = "test-client-id",
            RedirectUri = "http://localhost",
            Authority = "https://login.microsoftonline.com/common",
            Scopes = ["test.scope"]
        };

    [Fact]
    public void ThrowArgumentNullExceptionWhenAuthClientIsNull()
    {
        var exception = Should.Throw<ArgumentNullException>(
            () => new AuthService(null!, CreateTestConfiguration())
        );

        exception.ParamName.ShouldBe("authClient");
    }

    [Fact]
    public async Task ReturnSuccessResultWhenLoginSucceeds()
    {
        var mockClient = Substitute.For<IAuthenticationClient>();
        var mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "access_token");

        mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(mockAuthResult);

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.LoginAsync(TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue();
        result.AccountId.ShouldBe("acc1");
        result.DisplayName.ShouldBe("user@example.com");
        result.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnFailureResultWhenLoginThrowsMsalException()
    {
        var mockClient = Substitute.For<IAuthenticationClient>();
        mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MsalAuthResult>(new MsalException("login_failed")));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.LoginAsync(TestContext.Current.CancellationToken);

        result.Success.ShouldBeFalse();
        result.AccountId.ShouldBeNull();
        result.DisplayName.ShouldBeNull();
        result.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnFailureResultWhenLoginIsCancelled()
    {
        var mockClient = Substitute.For<IAuthenticationClient>();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        mockClient.AcquireTokenInteractiveAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MsalAuthResult>(new OperationCanceledException()));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.LoginAsync(cts.Token);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Login was cancelled.");
    }

    [Fact]
    public async Task ReturnTrueWhenLogoutSucceeds()
    {
        var mockClient = Substitute.For<IAuthenticationClient>();
        var mockAccount = CreateMockAccount("acc1", "user@example.com");

        mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        mockClient.RemoveAsync(mockAccount, TestContext.Current.CancellationToken).Returns(Task.CompletedTask);

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.LogoutAsync("acc1", TestContext.Current.CancellationToken);

        result.ShouldBeTrue();
        await mockClient.Received(1).RemoveAsync(mockAccount, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ReturnFalseWhenLogoutAccountNotFound()
    {
        var mockClient = Substitute.For<IAuthenticationClient>();

        mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IEnumerable<IAccount>>([]));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.LogoutAsync("nonexistent", TestContext.Current.CancellationToken);
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnEmptyListWhenNoAccountsAuthenticated()
    {
        var mockClient = Substitute.For<IAuthenticationClient>();
        mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IEnumerable<IAccount>>([]));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.GetAuthenticatedAccountsAsync(TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ReturnAuthenticatedAccountsCorrectly()
    {
        var mockClient = Substitute.For<IAuthenticationClient>();
        var account1 = CreateMockAccount("acc1", "user1@example.com");
        var account2 = CreateMockAccount("acc2", "user2@example.com");

        mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IEnumerable<IAccount>>([account1, account2]));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.GetAuthenticatedAccountsAsync(TestContext.Current.CancellationToken);
        result.Count.ShouldBe(2);
        result[0].AccountId.ShouldBe("acc1");
        result[0].DisplayName.ShouldBe("user1@example.com");
        result[1].AccountId.ShouldBe("acc2");
        result[1].DisplayName.ShouldBe("user2@example.com");
    }

    [Fact]
    public async Task ReturnAccessTokenWhenGetAccessTokenSucceeds()
    {
        var mockClient = Substitute.For<IAuthenticationClient>();
        var mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "token123");

        mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(mockAuthResult);

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.GetAccessTokenAsync("acc1", TestContext.Current.CancellationToken);

        result.ShouldBe("token123");
    }

    [Fact]
    public async Task ReturnNullWhenGetAccessTokenAccountNotFound()
    {
        var mockClient = Substitute.For<IAuthenticationClient>();
        mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IEnumerable<IAccount>>([]));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.GetAccessTokenAsync("nonexistent", TestContext.Current.CancellationToken);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnNullWhenGetAccessTokenThrowsMsalUiRequiredException()
    {
        var mockClient = Substitute.For<IAuthenticationClient>();
        var mockAccount = CreateMockAccount("acc1", "user@example.com");

        mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<MsalAuthResult>(
                new MsalUiRequiredException("error_code", "User interaction required")));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.GetAccessTokenAsync("acc1", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task ReturnFalseWhenAccountNotAuthenticated()
    {
        var mockClient = Substitute.For<IAuthenticationClient>();
        mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IEnumerable<IAccount>>([]));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.IsAuthenticatedAsync("acc1", TestContext.Current.CancellationToken);
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnTrueWhenAccountIsAuthenticated()
    {
        var mockClient = Substitute.For<IAuthenticationClient>();
        var mockAccount = CreateMockAccount("acc1", "user@example.com");

        mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.IsAuthenticatedAsync("acc1", TestContext.Current.CancellationToken);
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task AcquireTokenSilentlyCallsGetAccessToken()
    {
        var mockClient = Substitute.For<IAuthenticationClient>();
        var mockAccount = CreateMockAccount("acc1", "user@example.com");
        var mockAuthResult = new MsalAuthResult(mockAccount, "token456");

        mockClient.GetAccountsAsync(TestContext.Current.CancellationToken).Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        mockClient.AcquireTokenSilentAsync(Arg.Any<IEnumerable<string>>(), mockAccount, Arg.Any<CancellationToken>())
            .Returns(mockAuthResult);

        var service = new AuthService(mockClient, CreateTestConfiguration());

        var result = await service.AcquireTokenSilentAsync("acc1", TestContext.Current.CancellationToken);

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
