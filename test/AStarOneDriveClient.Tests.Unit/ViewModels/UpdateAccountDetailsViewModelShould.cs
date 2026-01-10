using System.Collections.ObjectModel;
using System.Reactive.Linq;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Repositories;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.ViewModels;

namespace AStarOneDriveClient.Tests.Unit.ViewModels;

public class UpdateAccountDetailsViewModelShould
{
    [Fact]
    public void ThrowArgumentNullExceptionWhenAccountRepositoryIsNull()
    {
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var exception = Should.Throw<ArgumentNullException>(() =>
            new UpdateAccountDetailsViewModel(null!, mockScheduler));

        exception.ParamName.ShouldBe("accountRepository");
    }

    [Fact]
    public void InitializeWithEmptyAccountsCollection()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.Accounts.ShouldBeOfType<ObservableCollection<AccountInfo>>();
        sut.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithNullSelectedAccount()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.SelectedAccount.ShouldBeNull();
    }

    [Fact]
    public void InitializeWithEmptyLocalSyncPath()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.LocalSyncPath.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithEnableDetailedSyncLoggingFalse()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.EnableDetailedSyncLogging.ShouldBeFalse();
    }

    [Fact]
    public void InitializeWithEnableDebugLoggingFalse()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.EnableDebugLogging.ShouldBeFalse();
    }

    [Fact]
    public void InitializeWithEmptyStatusMessage()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.StatusMessage.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeWithIsSuccessFalse()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void InitializeUpdateCommand()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.UpdateCommand.ShouldNotBeNull();
    }

    [Fact]
    public void InitializeCancelCommand()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.CancelCommand.ShouldNotBeNull();
    }

    [Fact]
    public void InitializeBrowseFolderCommand()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.BrowseFolderCommand.ShouldNotBeNull();
    }

    [Fact]
    public void LoadEditableFieldsWhenAccountIsSelected()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            "delta-token",
            true,
            false,
            3,
            50,
            null);

        sut.SelectedAccount = account;

        sut.LocalSyncPath.ShouldBe(@"C:\TestPath");
        sut.EnableDetailedSyncLogging.ShouldBeTrue();
        sut.StatusMessage.ShouldBeEmpty();
    }

    [Fact]
    public void LoadEnableDebugLoggingWhenAccountIsSelected()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            "delta-token",
            false,
            true,
            3,
            50,
            null);

        sut.SelectedAccount = account;

        sut.EnableDebugLogging.ShouldBeTrue();
    }

    [Fact]
    public void ClearStatusMessageWhenAccountIsSelected()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
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
            false,
            3,
            50,
            null);

        sut.SelectedAccount = account;

        sut.StatusMessage.ShouldBeEmpty();
    }

    [Fact]
    public void RaisePropertyChangedWhenSelectedAccountChanges()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
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
            false,
            3,
            50,
            null);

        sut.SelectedAccount = account;

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenLocalSyncPathChanges()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
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
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
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
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
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
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
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
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
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
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var eventRaised = false;

        sut.RequestClose += (_, _) => eventRaised = true;

        sut.CancelCommand.Execute().Subscribe();

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void NotLoadEditableFieldsWhenAccountIsSetToNull()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            LocalSyncPath = @"C:\ExistingPath",
            EnableDetailedSyncLogging = true,
            SelectedAccount = null
        };

        sut.LocalSyncPath.ShouldBe(@"C:\ExistingPath");
        sut.EnableDetailedSyncLogging.ShouldBeTrue();
    }

    [Fact]
    public void DisableUpdateCommandWhenNoAccountIsSelected()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        var canExecute = false;
        sut.UpdateCommand.CanExecute.Subscribe(x => canExecute = x);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void DisableUpdateCommandWhenLocalSyncPathIsEmpty()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            null);

        sut.SelectedAccount = account;
        sut.LocalSyncPath = string.Empty;

        var canExecute = true;
        sut.UpdateCommand.CanExecute.Subscribe(x => canExecute = x);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void EnableUpdateCommandWhenAccountIsSelectedAndPathIsNotEmpty()
    {
        var mockRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        var account = new AccountInfo(
            "account-123",
            "Test User",
            @"C:\TestPath",
            true,
            DateTime.UtcNow,
            null,
            false,
            false,
            3,
            50,
            null);

        sut.SelectedAccount = account;
        sut.LocalSyncPath = @"C:\ValidPath";

        var canExecute = false;
        sut.UpdateCommand.CanExecute.Subscribe(x => canExecute = x);

        canExecute.ShouldBeTrue();
    }

    [Fact]
    public void ClampMaxParallelUpDownloadsToMinimumOf1()
    {
        var mockAccountRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockAccountRepo, mockScheduler)
        {
            MaxParallelUpDownloads = 0
        };

        sut.MaxParallelUpDownloads.ShouldBe(1);
    }

    [Fact]
    public void ClampMaxParallelUpDownloadsToMaximumOf10()
    {
        var mockAccountRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockAccountRepo, mockScheduler)
        {
            MaxParallelUpDownloads = 20
        };

        sut.MaxParallelUpDownloads.ShouldBe(10);
    }

    [Fact]
    public void ClampMaxItemsInBatchToMinimumOf1()
    {
        var mockAccountRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockAccountRepo, mockScheduler)
        {
            MaxItemsInBatch = 0
        };

        sut.MaxItemsInBatch.ShouldBe(1);
    }

    [Fact]
    public void ClampMaxItemsInBatchToMaximumOf100()
    {
        var mockAccountRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockAccountRepo, mockScheduler)
        {
            MaxItemsInBatch = 200
        };

        sut.MaxItemsInBatch.ShouldBe(100);
    }

    [Fact]
    public void LoadMaxParallelUpDownloadsWhenAccountSelected()
    {
        var mockAccountRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockAccountRepo, mockScheduler);
        var account = new AccountInfo(
            "acc1",
            "Test User",
            @"C:\Sync",
            true,
            null,
            null,
            false,
            false,
            5,
            75,
            null);

        sut.SelectedAccount = account;

        sut.MaxParallelUpDownloads.ShouldBe(5);
    }

    [Fact]
    public void LoadMaxItemsInBatchWhenAccountSelected()
    {
        var mockAccountRepo = Substitute.For<IAccountRepository>();
        var mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockAccountRepo, mockScheduler);
        var account = new AccountInfo(
            "acc1",
            "Test User",
            @"C:\Sync",
            true,
            null,
            null,
            false,
            false,
            5,
            75,
            null);

        sut.SelectedAccount = account;

        sut.MaxItemsInBatch.ShouldBe(75);
    }
}
