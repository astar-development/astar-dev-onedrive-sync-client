using System.Collections.ObjectModel;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.ViewModels;

namespace AStarOneDriveClient.Tests.Unit.ViewModels;

public class UpdateAccountDetailsViewModelShould
{
    [Fact]
    public void ThrowArgumentNullExceptionWhenAccountRepositoryIsNull()
    {
        var exception = Should.Throw<ArgumentNullException>(() =>
            new UpdateAccountDetailsViewModel(null!));

        exception.ParamName.ShouldBe("accountRepository");
    }

    [Fact]
    public void InitializeWithEmptyAccountsCollection()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        sut.Accounts.ShouldBeOfType<ObservableCollection<AccountInfo>>();
        sut.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithNullSelectedAccount()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        sut.SelectedAccount.ShouldBeNull();
    }

    [Fact]
    public void InitializeWithEmptyLocalSyncPath()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        sut.LocalSyncPath.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithEnableDetailedSyncLoggingFalse()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        sut.EnableDetailedSyncLogging.ShouldBeFalse();
    }

    [Fact]
    public void InitializeWithEnableDebugLoggingFalse()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        sut.EnableDebugLogging.ShouldBeFalse();
    }

    [Fact]
    public void InitializeWithEmptyStatusMessage()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        sut.StatusMessage.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithIsSuccessFalse()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        sut.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void InitializeUpdateCommand()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        sut.UpdateCommand.ShouldNotBeNull();
    }

    [Fact]
    public void InitializeCancelCommand()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        sut.CancelCommand.ShouldNotBeNull();
    }

    [Fact]
    public void InitializeBrowseFolderCommand()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        sut.BrowseFolderCommand.ShouldNotBeNull();
    }

    [Fact]
    public void LoadEditableFieldsWhenAccountIsSelected()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            "delta-token",
            true,
            false);

        sut.SelectedAccount = account;

        sut.LocalSyncPath.ShouldBe(@"C:\TestPath");
        sut.EnableDetailedSyncLogging.ShouldBeTrue();
        sut.StatusMessage.ShouldBeEmpty();
    }

    [Fact]
    public void LoadEnableDebugLoggingWhenAccountIsSelected()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            "delta-token",
            false,
            true);

        sut.SelectedAccount = account;

        sut.EnableDebugLogging.ShouldBeTrue();
    }

    [Fact]
    public void ClearStatusMessageWhenAccountIsSelected()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo)
        {
            StatusMessage = "Previous message"
        };

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            null,
            false,
            false);

        sut.SelectedAccount = account;

        sut.StatusMessage.ShouldBeEmpty();
    }

    [Fact]
    public void RaisePropertyChangedWhenSelectedAccountChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(UpdateAccountDetailsViewModel.SelectedAccount))
            {
                propertyChanged = true;
            }
        };

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            null,
            false,
            false);

        sut.SelectedAccount = account;

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenLocalSyncPathChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(UpdateAccountDetailsViewModel.LocalSyncPath))
            {
                propertyChanged = true;
            }
        };

        sut.LocalSyncPath = @"C:\NewPath";

        propertyChanged.ShouldBeTrue();
        sut.LocalSyncPath.ShouldBe(@"C:\NewPath");
    }

    [Fact]
    public void RaisePropertyChangedWhenEnableDetailedSyncLoggingChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(UpdateAccountDetailsViewModel.EnableDetailedSyncLogging))
            {
                propertyChanged = true;
            }
        };

        sut.EnableDetailedSyncLogging = true;

        propertyChanged.ShouldBeTrue();
        sut.EnableDetailedSyncLogging.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenEnableDebugLoggingChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(UpdateAccountDetailsViewModel.EnableDebugLogging))
            {
                propertyChanged = true;
            }
        };

        sut.EnableDebugLogging = true;

        propertyChanged.ShouldBeTrue();
        sut.EnableDebugLogging.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenStatusMessageChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(UpdateAccountDetailsViewModel.StatusMessage))
            {
                propertyChanged = true;
            }
        };

        sut.StatusMessage = "Test message";

        propertyChanged.ShouldBeTrue();
        sut.StatusMessage.ShouldBe("Test message");
    }

    [Fact]
    public void RaisePropertyChangedWhenIsSuccessChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(UpdateAccountDetailsViewModel.IsSuccess))
            {
                propertyChanged = true;
            }
        };

        sut.IsSuccess = true;

        propertyChanged.ShouldBeTrue();
        sut.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void ExecuteCancelCommandAndRaiseRequestCloseEvent()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo);
        var eventRaised = false;

        sut.RequestClose += (_, _) => eventRaised = true;

        sut.CancelCommand.Execute().Subscribe();

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void NotLoadEditableFieldsWhenAccountIsSetToNull()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo)
        {
            LocalSyncPath = @"C:\ExistingPath",
            EnableDetailedSyncLogging = true
        };

        sut.SelectedAccount = null;

        sut.LocalSyncPath.ShouldBe(@"C:\ExistingPath");
        sut.EnableDetailedSyncLogging.ShouldBeTrue();
    }

    [Fact]
    public void DisableUpdateCommandWhenNoAccountIsSelected()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        var canExecute = false;
        sut.UpdateCommand.CanExecute.Subscribe(x => canExecute = x);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void DisableUpdateCommandWhenLocalSyncPathIsEmpty()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            null,
            false,
            false);

        sut.SelectedAccount = account;
        sut.LocalSyncPath = string.Empty;

        var canExecute = true;
        sut.UpdateCommand.CanExecute.Subscribe(x => canExecute = x);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void EnableUpdateCommandWhenAccountIsSelectedAndPathIsNotEmpty()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo);

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            null,
            false,
            false);

        sut.SelectedAccount = account;
        sut.LocalSyncPath = @"C:\ValidPath";

        var canExecute = false;
        sut.UpdateCommand.CanExecute.Subscribe(x => canExecute = x);

        canExecute.ShouldBeTrue();
    }
}
