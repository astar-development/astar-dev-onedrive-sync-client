using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.SecureStorage;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using NSubstitute;
using Shouldly;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.Authentication.Services;

public class AuthenticationServiceShould
{
    [Fact]
    public async Task ReturnOkResultWithAuthTokenWhenAuthenticateAsyncSucceeds()
    {
        IPublicClientApplication mockPublicClient = Substitute.For<IPublicClientApplication>();
        ISecureTokenStorage mockSecureStorage = Substitute.For<ISecureTokenStorage>();
        ILogger<AuthenticationService> mockLogger = Substitute.For<ILogger<AuthenticationService>>();
        AcquireTokenWithDeviceCodeParameterBuilder mockBuilder = Substitute.For<AcquireTokenWithDeviceCodeParameterBuilder>();

        var authResult = new AuthenticationResult("token123", false, string.Empty, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(1), string.Empty, null, string.Empty, [], Guid.Empty);
        
        mockBuilder.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));
        mockPublicClient.AcquireTokenWithDeviceCode(Arg.Any<IEnumerable<string>>(), Arg.Any<Func<DeviceCodeResult, Task>>())
            .Returns(mockBuilder);

        var service = new AuthenticationService(mockPublicClient, mockSecureStorage, mockLogger);

        Result<AuthToken, AuthenticationError> result = await service.AuthenticateAsync();

        _ = result.ShouldBeOfType<Result<AuthToken, AuthenticationError>.Ok>();
        var okResult = (Result<AuthToken, AuthenticationError>.Ok)result;
        okResult.Value.AccessToken.ShouldBe("token123");
        okResult.Value.IsExpired.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnTimedOutErrorWhenAuthenticateAsyncExceedsTimeout()
    {
        IPublicClientApplication mockPublicClient = Substitute.For<IPublicClientApplication>();
        ISecureTokenStorage mockSecureStorage = Substitute.For<ISecureTokenStorage>();
        ILogger<AuthenticationService> mockLogger = Substitute.For<ILogger<AuthenticationService>>();
        AcquireTokenWithDeviceCodeParameterBuilder mockBuilder = Substitute.For<AcquireTokenWithDeviceCodeParameterBuilder>();

        mockBuilder.ExecuteAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        
        mockPublicClient.AcquireTokenWithDeviceCode(Arg.Any<IEnumerable<string>>(), Arg.Any<Func<DeviceCodeResult, Task>>())
            .Returns(mockBuilder);

        var service = new AuthenticationService(mockPublicClient, mockSecureStorage, mockLogger);

        Result<AuthToken, AuthenticationError> result = await service.AuthenticateAsync();

        _ = result.ShouldBeOfType<Result<AuthToken, AuthenticationError>.Error>();
        var errorResult = (Result<AuthToken, AuthenticationError>.Error)result;
        _ = errorResult.Reason.ShouldBeOfType<AuthenticationError.TimedOut>();

        mockBuilder.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(_ => throw new OperationCanceledException());
        mockPublicClient.AcquireTokenWithDeviceCode(Arg.Any<IEnumerable<string>>(), Arg.Any<Func<DeviceCodeResult, Task>>())
            .Returns(mockBuilder);

        var service = new AuthenticationService(mockPublicClient, mockSecureStorage, mockLogger);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        Result<AuthToken, AuthenticationError> result = await service.AuthenticateAsync(cts.Token);

        _ = result.ShouldBeOfType<Result<AuthToken, AuthenticationError>.Error>();
        var errorResult = (Result<AuthToken, AuthenticationError>.Error)result;
        _ = errorResult.Reason.ShouldBeOfType<AuthenticationError.Cancelled>();
    }

    [Fact]
    public async Task ReturnOkResultWithRefreshedTokenWhenRefreshTokenAsyncSucceeds()
    {
        IPublicClientApplication mockPublicClient = Substitute.For<IPublicClientApplication>();
        ISecureTokenStorage mockSecureStorage = Substitute.For<ISecureTokenStorage>();
        ILogger<AuthenticationService> mockLogger = Substitute.For<ILogger<AuthenticationService>>();
        AcquireTokenSilentParameterBuilder mockBuilder = Substitute.For<AcquireTokenSilentParameterBuilder>();
        IAccount mockAccount = Substitute.For<IAccount>();

        var authResult = new AuthenticationResult("new_token", false, string.Empty, DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddHours(1), string.Empty, null, string.Empty, [], Guid.Empty);
        
        mockBuilder.ExecuteAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(authResult));
        mockPublicClient.GetAccountsAsync().Returns(Task.FromResult<IEnumerable<IAccount>>([mockAccount]));
        mockPublicClient.AcquireTokenSilent(Arg.Any<IEnumerable<string>>(), Arg.Any<IAccount>())
            .Returns(mockBuilder);

        var service = new AuthenticationService(mockPublicClient, mockSecureStorage, mockLogger);

        Result<AuthToken, AuthenticationError> result = await service.RefreshTokenAsync("account@example.com");

        _ = result.ShouldBeOfType<Result<AuthToken, AuthenticationError>.Ok>();
        var okResult = (Result<AuthToken, AuthenticationError>.Ok)result;
        okResult.Value.AccessToken.ShouldBe("new_token");
    }

    [Fact]
    public async Task ReturnServiceErrorWhenAuthenticateAsyncFailsWithMsalException()
    {
        IPublicClientApplication mockPublicClient = Substitute.For<IPublicClientApplication>();
        ISecureTokenStorage mockSecureStorage = Substitute.For<ISecureTokenStorage>();
        ILogger<AuthenticationService> mockLogger = Substitute.For<ILogger<AuthenticationService>>();
        AcquireTokenWithDeviceCodeParameterBuilder mockBuilder = Substitute.For<AcquireTokenWithDeviceCodeParameterBuilder>();

        var exception = new MsalServiceException("unauthorized_client", "App configuration error");
        mockBuilder.ExecuteAsync(Arg.Any<CancellationToken>()).Returns<AuthenticationResult>(_ => throw exception);
        mockPublicClient.AcquireTokenWithDeviceCode(Arg.Any<IEnumerable<string>>(), Arg.Any<Func<DeviceCodeResult, Task>>())
            .Returns(mockBuilder);

        var service = new AuthenticationService(mockPublicClient, mockSecureStorage, mockLogger);

        Result<AuthToken, AuthenticationError> result = await service.AuthenticateAsync();

        _ = result.ShouldBeOfType<Result<AuthToken, AuthenticationError>.Error>();
        var errorResult = (Result<AuthToken, AuthenticationError>.Error)result;
        _ = errorResult.Reason.ShouldBeOfType<AuthenticationError.ServiceError>();
    }
}
