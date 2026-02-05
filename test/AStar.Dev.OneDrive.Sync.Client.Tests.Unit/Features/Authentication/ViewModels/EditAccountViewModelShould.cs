using System;
using System.Threading.Tasks;
using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Services;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.ViewModels;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.Authentication.ViewModels;

public class EditAccountViewModelShould
{
    private readonly IAccountManagementService _accountManagementService = Substitute.For<IAccountManagementService>();
    private readonly EditAccountViewModel _viewModel;

    public EditAccountViewModelShould() => _viewModel = new EditAccountViewModel(_accountManagementService);

    [Fact]
    public void ThrowArgumentNullExceptionWhenAccountManagementServiceIsNull()
    {
        Func<EditAccountViewModel> act = () => new EditAccountViewModel(null!);

        act.ShouldThrow<ArgumentNullException>();
    }

    [Fact]
    public void InitializeWithEmptyProperties()
    {
        _viewModel.HashedAccountId.ShouldBeNullOrWhiteSpace();
        _viewModel.HomeSyncDirectory.ShouldBeNullOrWhiteSpace();
        _viewModel.MaxConcurrent.ShouldBe(5);
        _viewModel.DebugLoggingEnabled.ShouldBeFalse();
        _viewModel.MaxBandwidthKBps.ShouldBeNull();
    }

    [Fact]
    public void LoadAccountSetsPropertiesFromAccount()
    {
        var account = new Account
        {
            HashedAccountId = "test-hashed-id",
            HomeSyncDirectory = "/test/path",
            MaxConcurrent = 10,
            DebugLoggingEnabled = true,
            MaxBandwidthKBps = 1024
        };

        _viewModel.LoadAccount(account);

        _viewModel.HashedAccountId.ShouldBe("test-hashed-id");
        _viewModel.HomeSyncDirectory.ShouldBe("/test/path");
        _viewModel.MaxConcurrent.ShouldBe(10);
        _viewModel.DebugLoggingEnabled.ShouldBeTrue();
        _viewModel.MaxBandwidthKBps.ShouldBe(1024);
    }

    [Fact]
    public void ValidateMaxConcurrentReturnsTrueForValidRange()
    {
        _viewModel.MaxConcurrent = 10;

        _viewModel.MaxConcurrentError.ShouldBeNullOrWhiteSpace();
    }

    [Fact]
    public void ValidateMaxConcurrentSetsErrorForValueLessThanOne()
    {
        _viewModel.MaxConcurrent = 0;

        _viewModel.MaxConcurrentError.ShouldNotBeNullOrWhiteSpace();
        _viewModel.MaxConcurrentError.ShouldContain("must be at least 1");
    }

    [Fact]
    public void ValidateMaxBandwidthSetsErrorForNegativeValue()
    {
        _viewModel.MaxBandwidthKBps = -100;

        _viewModel.MaxBandwidthError.ShouldNotBeNullOrWhiteSpace();
        _viewModel.MaxBandwidthError.ShouldContain("cannot be negative");
    }

    [Fact]
    public void ValidateMaxBandwidthClearsErrorForNullValue()
    {
        _viewModel.MaxBandwidthKBps = null;

        _viewModel.MaxBandwidthError.ShouldBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SaveCommandUpdatesAllSettingsSuccessfully()
    {
        var account = new Account { HashedAccountId = "test-id", MaxConcurrent = 5 };
        _viewModel.LoadAccount(account);
        _viewModel.HomeSyncDirectory = "/new/path";
        _viewModel.MaxConcurrent = 15;
        _viewModel.DebugLoggingEnabled = true;
        _viewModel.MaxBandwidthKBps = 2048;

        _accountManagementService.UpdateHomeSyncDirectoryAsync("test-id", "/new/path")
            .Returns(new Result<Account, AccountManagementError>.Ok(account));
        _accountManagementService.UpdateMaxConcurrentAsync("test-id", 15)
            .Returns(new Result<Account, AccountManagementError>.Ok(account));
        _accountManagementService.UpdateDebugLoggingAsync("test-id", true)
            .Returns(new Result<Account, AccountManagementError>.Ok(account));
        _accountManagementService.UpdateMaxBandwidthKBpsAsync("test-id", 2048)
            .Returns(new Result<Account, AccountManagementError>.Ok(account));

        _viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.SaveSuccessful.ShouldBeTrue();
        _viewModel.ErrorMessage.ShouldBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SaveCommandSetsErrorWhenValidationFails()
    {
        var account = new Account { HashedAccountId = "test-id" };
        _viewModel.LoadAccount(account);
        _viewModel.MaxConcurrent = 0;

        _viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.SaveSuccessful.ShouldBeFalse();
        _viewModel.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
        _viewModel.ErrorMessage!.ShouldContain("validation");
    }

    [Fact]
    public async Task SaveCommandSetsErrorWhenServiceReturnsError()
    {
        var account = new Account { HashedAccountId = "test-id" };
        _viewModel.LoadAccount(account);

        _accountManagementService.UpdateHomeSyncDirectoryAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns(new Result<Account, AccountManagementError>.Error(AccountManagementError.RepositoryError));

        _viewModel.SaveCommand.Execute(null);
        await Task.Delay(100);

        _viewModel.SaveSuccessful.ShouldBeFalse();
        _viewModel.ErrorMessage.ShouldNotBeNullOrWhiteSpace();
    }
}
