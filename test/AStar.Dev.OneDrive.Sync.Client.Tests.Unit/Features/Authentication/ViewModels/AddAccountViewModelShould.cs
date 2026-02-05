using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.ViewModels;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.Authentication.ViewModels;

public class AddAccountViewModelShould
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAccountCreationService _accountCreationService;
    private readonly AddAccountViewModel _viewModel;

    public AddAccountViewModelShould()
    {
        _authenticationService = Substitute.For<IAuthenticationService>();
        _accountCreationService = Substitute.For<IAccountCreationService>();
        _viewModel = new AddAccountViewModel(_authenticationService, _accountCreationService);
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenAuthenticationServiceIsNull() => Should.Throw<ArgumentNullException>(() => new AddAccountViewModel(null!, _accountCreationService));

    [Fact]
    public void ThrowArgumentNullExceptionWhenAccountCreationServiceIsNull() => Should.Throw<ArgumentNullException>(() => new AddAccountViewModel(_authenticationService, null!));

    [Fact]
    public void InitializeWithEmptyState()
    {
        _viewModel.StatusMessage.ShouldBe(string.Empty);
        _viewModel.ErrorMessage.ShouldBe(string.Empty);
        _viewModel.IsAuthenticating.ShouldBeFalse();
        _viewModel.IsCreatingAccount.ShouldBeFalse();
        _viewModel.CreatedAccount.ShouldBeNull();
        _viewModel.AuthenticateCommand.ShouldNotBeNull();
    }

    [Fact]
    public async Task CreateAccountSuccessfullyWhenAuthenticationSucceeds()
    {
        var authToken = new AuthToken("access-token", DateTime.UtcNow.AddHours(1));
        var account = new Account
        {
            Id = Guid.NewGuid(),
            HashedEmail = "hashed-email@example.com",
            HashedAccountId = "hashed-account-id"
        };

        _authenticationService.AuthenticateAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<AuthToken, AuthenticationError>.Ok(authToken));
        _accountCreationService.CreateAccountAsync(Arg.Any<AuthToken>(), Arg.Any<CancellationToken>())
            .Returns(new Result<Account, AccountCreationError>.Ok(account));

        _viewModel.AuthenticateCommand.Execute(null);

        // Allow async operations to complete
        await Task.Delay(100);

        _viewModel.CreatedAccount.ShouldNotBeNull();
        _viewModel.CreatedAccount.ShouldBe(account);
        _viewModel.ErrorMessage.ShouldBe(string.Empty);
        _viewModel.IsAuthenticating.ShouldBeFalse();
        _viewModel.IsCreatingAccount.ShouldBeFalse();
    }

    [Fact]
    public async Task SetErrorMessageWhenAuthenticationCancelled()
    {
        _authenticationService.AuthenticateAsync(Arg.Any<CancellationToken>())
            .Returns(new Result<AuthToken, AuthenticationError>.Error(new AuthenticationError.Cancelled()));

        _viewModel.AuthenticateCommand.Execute(null);

        // Allow async operations to complete
        await Task.Delay(100);

        _viewModel.ErrorMessage.ShouldContain("cancelled");
        _viewModel.StatusMessage.ShouldBe("Authentication failed");
        _viewModel.IsAuthenticating.ShouldBeFalse();
        _viewModel.CreatedAccount.ShouldBeNull();

        await _accountCreationService.DidNotReceive().CreateAccountAsync(Arg.Any<AuthToken>(), Arg.Any<CancellationToken>());
    }
}
