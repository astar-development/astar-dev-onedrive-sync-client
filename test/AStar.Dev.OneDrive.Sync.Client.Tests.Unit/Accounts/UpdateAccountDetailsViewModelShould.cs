using AStar.Dev.OneDrive.Sync.Client.Accounts;
using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Accounts;

/// <summary>
///     Unit tests for <see cref="UpdateAccountDetailsViewModel" />.
/// </summary>
public sealed class UpdateAccountDetailsViewModelShould
{
    [Fact]
    public async Task LoadAccountsFromRepositoryOnInitialization()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        AccountInfo account1 = CreateTestAccount("acc1", "User 1");
        AccountInfo account2 = CreateTestAccount("acc2", "User 2");
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([account1, account2]));
        _ = mockRepo.GetByIdAsync(Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>()).Returns((AccountInfo?)null);

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.Accounts.Count.ShouldBe(2);
        sut.Accounts[0].DisplayName.ShouldBe("User 1");
        sut.Accounts[1].DisplayName.ShouldBe("User 2");
    }

    [Fact]
    public async Task HandleExceptionWhenLoadingAccountsFails()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns<IReadOnlyList<AccountInfo>>(_ => throw new Exception("Database error"));

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.StatusMessage.ShouldContain("Failed to load accounts");
        sut.IsSuccess.ShouldBeFalse();
        sut.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task UpdateLastSyncUtcFromRepositoryWhenLoadingAccounts()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        DateTime lastSync = DateTime.UtcNow.AddHours(-2);
        AccountInfo account = CreateTestAccount("acc1", "User 1");
        AccountInfo accountWithSync = account with { LastSyncUtc = lastSync };
        _ = mockRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<AccountInfo>>([account]));
        _ = mockRepo.GetByIdAsync(account.HashedAccountId, Arg.Any<CancellationToken>()).Returns(accountWithSync);

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        await Task.Delay(100, TestContext.Current.CancellationToken);

        sut.Accounts[0].LastSyncUtc.ShouldBe(lastSync);
    }

    [Fact]
    public async Task UpdateAccountSuccessfully()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(tempPath);

        try
        {
            IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
            IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
            AccountInfo account = CreateTestAccount("acc1", "User 1", tempPath);
            _ = mockRepo.UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
            {
                SelectedAccount = account,
                LocalSyncPath = tempPath,
                EnableDetailedSyncLogging = true,
                EnableDebugLogging = true,
                MaxParallelUpDownloads = 7,
                MaxItemsInBatch = 80,
                AutoSyncIntervalMinutes = 120
            };

            _ = sut.UpdateCommand.Execute().Subscribe();
            await Task.Delay(100, TestContext.Current.CancellationToken);

            await mockRepo.Received(1).UpdateAsync(Arg.Is<AccountInfo>(a =>
                a.LocalSyncPath == tempPath &&
                a.EnableDetailedSyncLogging &&
                a.EnableDebugLogging &&
                a.MaxParallelUpDownloads == 7 &&
                a.MaxItemsInBatch == 80 &&
                a.AutoSyncIntervalMinutes == 120), Arg.Any<CancellationToken>());
            sut.StatusMessage.ShouldBe("Account updated successfully!");
            sut.IsSuccess.ShouldBeTrue();
        }
        finally
        {
            if(Directory.Exists(tempPath))
                Directory.Delete(tempPath);
        }
    }

    [Fact]
    public async Task UpdateSchedulerServiceWithNewAutoSyncInterval()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(tempPath);

        try
        {
            IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
            IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
            AccountInfo account = CreateTestAccount("acc1", "User 1", tempPath);
            _ = mockRepo.UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
            {
                SelectedAccount = account,
                AutoSyncIntervalMinutes = 180
            };

            _ = sut.UpdateCommand.Execute().Subscribe();
            await Task.Delay(100, TestContext.Current.CancellationToken);

            mockScheduler.Received(1).UpdateSchedule(account.Id, account.HashedAccountId, 180);
        }
        finally
        {
            if(Directory.Exists(tempPath))
                Directory.Delete(tempPath);
        }
    }

    [Fact]
    public async Task UpdateAccountInCollectionAfterSuccessfulUpdate()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(tempPath);

        try
        {
            IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
            IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
            AccountInfo account = CreateTestAccount("acc1", "User 1", tempPath);
            _ = mockRepo.UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
            sut.Accounts.Add(account);
            sut.SelectedAccount = account;
            sut.EnableDetailedSyncLogging = true;

            _ = sut.UpdateCommand.Execute().Subscribe();
            await Task.Delay(100, TestContext.Current.CancellationToken);

            sut.Accounts[0].EnableDetailedSyncLogging.ShouldBeTrue();
            sut.SelectedAccount!.EnableDetailedSyncLogging.ShouldBeTrue();
        }
        finally
        {
            if(Directory.Exists(tempPath))
                Directory.Delete(tempPath);
        }
    }

    [Fact]
    public async Task RaiseRequestCloseEventAfterSuccessfulUpdate()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(tempPath);

        try
        {
            IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
            IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
            AccountInfo account = CreateTestAccount("acc1", "User 1", tempPath);
            _ = mockRepo.UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
            var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
            {
                SelectedAccount = account
            };
            var eventRaised = false;
            sut.RequestClose += (_, _) => eventRaised = true;

            _ = sut.UpdateCommand.Execute().Subscribe();
            await Task.Delay(2100, TestContext.Current.CancellationToken);

            eventRaised.ShouldBeTrue();
        }
        finally
        {
            if(Directory.Exists(tempPath))
                Directory.Delete(tempPath);
        }
    }

    [Fact]
    public async Task NotUpdateAccountWhenSelectedAccountIsNull()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            SelectedAccount = null
        };

        _ = sut.UpdateCommand.Execute().Subscribe();

        await mockRepo.DidNotReceive().UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShowErrorMessageWhenLocalSyncPathDoesNotExist()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        AccountInfo account = CreateTestAccount("acc1", "User 1");
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            SelectedAccount = account,
            LocalSyncPath = @"C:\NonExistentPath\Invalid"
        };

        _ = sut.UpdateCommand.Execute().Subscribe();

        sut.StatusMessage.ShouldContain("Local sync path does not exist");
        sut.IsSuccess.ShouldBeFalse();
        await mockRepo.DidNotReceive().UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ShowErrorMessageWhenUpdateFails()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _ = Directory.CreateDirectory(tempPath);

        try
        {
            IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
            IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
            AccountInfo account = CreateTestAccount("acc1", "User 1", tempPath);
            _ = mockRepo.UpdateAsync(Arg.Any<AccountInfo>(), Arg.Any<CancellationToken>()).Returns<Task>(_ => throw new Exception("Database error"));
            var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
            {
                SelectedAccount = account
            };
            var eventRaised = false;
            sut.RequestClose += (_, _) => eventRaised = true;

            _ = sut.UpdateCommand.Execute().Subscribe();
            await Task.Delay(100, TestContext.Current.CancellationToken);

            sut.StatusMessage.ShouldContain("Failed to update account");
            sut.IsSuccess.ShouldBeFalse();
            eventRaised.ShouldBeFalse();
        }
        finally
        {
            if(Directory.Exists(tempPath))
                Directory.Delete(tempPath);
        }
    }

    [Fact]
    public void ClampAutoSyncIntervalMinutesToMinimumOf60WhenGreaterThanZero()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            AutoSyncIntervalMinutes = 30
        };

        sut.AutoSyncIntervalMinutes.ShouldBe(60);
    }

    [Fact]
    public void ClampAutoSyncIntervalMinutesToMaximumOf1440()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            AutoSyncIntervalMinutes = 2000
        };

        sut.AutoSyncIntervalMinutes.ShouldBe(1440);
    }

    [Fact]
    public void AllowAutoSyncIntervalMinutesOfZeroToDisableAutoSync()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            AutoSyncIntervalMinutes = 0
        };

        sut.AutoSyncIntervalMinutes.ShouldBe(0);
    }

    [Fact]
    public void AllowAutoSyncIntervalMinutesOfNegativeOneToDisableAutoSync()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            AutoSyncIntervalMinutes = -1
        };

        sut.AutoSyncIntervalMinutes.ShouldBe(0);
    }

    [Fact]
    public void LoadAutoSyncIntervalMinutesWhenAccountSelected()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        AccountInfo account = CreateTestAccount("acc1", "User 1", autoSyncInterval: 240);

        sut.SelectedAccount = account;

        sut.AutoSyncIntervalMinutes.ShouldBe(240);
    }

    [Fact]
    public void LoadAutoSyncIntervalMinutesAsZeroWhenAccountHasNullInterval()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        AccountInfo account = CreateTestAccount("acc1", "User 1", autoSyncInterval: null);

        sut.SelectedAccount = account;

        sut.AutoSyncIntervalMinutes.ShouldBe(0);
    }

    [Fact]
    public void RaisePropertyChangedWhenMaxParallelUpDownloadsChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.MaxParallelUpDownloads))
                propertyChanged = true;
        };

        sut.MaxParallelUpDownloads = 8;

        propertyChanged.ShouldBeTrue();
        sut.MaxParallelUpDownloads.ShouldBe(8);
    }

    [Fact]
    public void RaisePropertyChangedWhenMaxItemsInBatchChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.MaxItemsInBatch))
                propertyChanged = true;
        };

        sut.MaxItemsInBatch = 90;

        propertyChanged.ShouldBeTrue();
        sut.MaxItemsInBatch.ShouldBe(90);
    }

    [Fact]
    public void RaisePropertyChangedWhenAutoSyncIntervalMinutesChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.AutoSyncIntervalMinutes))
                propertyChanged = true;
        };

        sut.AutoSyncIntervalMinutes = 120;

        propertyChanged.ShouldBeTrue();
        sut.AutoSyncIntervalMinutes.ShouldBe(120);
    }

    [Fact]
    public void InitializeMaxParallelUpDownloadsToDefaultValue()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.MaxParallelUpDownloads.ShouldBe(5);
    }

    [Fact]
    public void InitializeMaxItemsInBatchToDefaultValue()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.MaxItemsInBatch.ShouldBe(50);
    }

    [Fact]
    public void InitializeAutoSyncIntervalMinutesToZero()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.AutoSyncIntervalMinutes.ShouldBe(0);
    }

    [Fact]
    public void ClampMaxParallelUpDownloadsToMinimumOf1()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            MaxParallelUpDownloads = 0
        };

        sut.MaxParallelUpDownloads.ShouldBe(1);
    }

    [Fact]
    public void ClampMaxParallelUpDownloadsToMaximumOf10()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            MaxParallelUpDownloads = 15
        };

        sut.MaxParallelUpDownloads.ShouldBe(10);
    }

    [Fact]
    public void ClampMaxItemsInBatchToMinimumOf1()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            MaxItemsInBatch = -5
        };

        sut.MaxItemsInBatch.ShouldBe(1);
    }

    [Fact]
    public void ClampMaxItemsInBatchToMaximumOf100()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            MaxItemsInBatch = 150
        };

        sut.MaxItemsInBatch.ShouldBe(100);
    }

    [Fact]
    public void RaiseRequestCloseEventWhenCancelCommandExecuted()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var eventRaised = false;
        sut.RequestClose += (_, _) => eventRaised = true;

        _ = sut.CancelCommand.Execute().Subscribe();

        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public void LoadPropertiesWhenAccountSelected()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        AccountInfo account = CreateTestAccount("acc1", "User 1", @"C:\SyncPath", 180);
        AccountInfo accountWithSettings = account with
        {
            EnableDetailedSyncLogging = true,
            EnableDebugLogging = true,
            MaxParallelUpDownloads = 7,
            MaxItemsInBatch = 75
        };

        sut.SelectedAccount = accountWithSettings;

        sut.LocalSyncPath.ShouldBe(@"C:\SyncPath");
        sut.EnableDetailedSyncLogging.ShouldBeTrue();
        sut.EnableDebugLogging.ShouldBeTrue();
        sut.MaxParallelUpDownloads.ShouldBe(7);
        sut.MaxItemsInBatch.ShouldBe(75);
        sut.AutoSyncIntervalMinutes.ShouldBe(180);
        sut.StatusMessage.ShouldBeEmpty();
    }

    [Fact]
    public void RaisePropertyChangedWhenSelectedAccountChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.SelectedAccount))
                propertyChanged = true;
        };

        sut.SelectedAccount = CreateTestAccount("acc1", "User 1");

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenLocalSyncPathChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.LocalSyncPath))
                propertyChanged = true;
        };

        sut.LocalSyncPath = @"C:\NewPath";

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenEnableDetailedSyncLoggingChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.EnableDetailedSyncLogging))
                propertyChanged = true;
        };

        sut.EnableDetailedSyncLogging = true;

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenEnableDebugLoggingChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.EnableDebugLogging))
                propertyChanged = true;
        };

        sut.EnableDebugLogging = true;

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenStatusMessageChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.StatusMessage))
                propertyChanged = true;
        };

        sut.StatusMessage = "Test message";

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void RaisePropertyChangedWhenIsSuccessChanges()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);
        var propertyChanged = false;

        sut.PropertyChanged += (_, args) =>
        {
            if(args.PropertyName == nameof(UpdateAccountDetailsViewModel.IsSuccess))
                propertyChanged = true;
        };

        sut.IsSuccess = true;

        propertyChanged.ShouldBeTrue();
    }

    [Fact]
    public void InitializeAccountsCollectionToEmpty()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.Accounts.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeLocalSyncPathToEmptyString()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.LocalSyncPath.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeStatusMessageToEmptyString()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.StatusMessage.ShouldBeEmpty();
    }

    [Fact]
    public void InitializeIsSuccessToFalse()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();

        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler);

        sut.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public void DisableUpdateCommandWhenNoAccountSelected()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            SelectedAccount = null,
            LocalSyncPath = @"C:\ValidPath"
        };

        var canExecute = false;
        using IDisposable subscription = sut.UpdateCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void DisableUpdateCommandWhenLocalSyncPathIsEmpty()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            SelectedAccount = CreateTestAccount("acc1", "User 1"),
            LocalSyncPath = string.Empty
        };

        var canExecute = true;
        using IDisposable subscription = sut.UpdateCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeFalse();
    }

    [Fact]
    public void EnableUpdateCommandWhenAccountSelectedAndPathProvided()
    {
        IAccountRepository mockRepo = Substitute.For<IAccountRepository>();
        IAutoSyncSchedulerService mockScheduler = Substitute.For<IAutoSyncSchedulerService>();
        var sut = new UpdateAccountDetailsViewModel(mockRepo, mockScheduler)
        {
            SelectedAccount = CreateTestAccount("acc1", "User 1"),
            LocalSyncPath = @"C:\ValidPath"
        };

        var canExecute = false;
        using IDisposable subscription = sut.UpdateCommand.CanExecute.Subscribe(value => canExecute = value);

        canExecute.ShouldBeTrue();
    }

    private static AccountInfo CreateTestAccount(
        string id,
        string displayName,
        string? localSyncPath = null,
        int? autoSyncInterval = 0) => new(
            id,
            new HashedAccountId(AccountIdHasher.Hash(id)),
            displayName,
            localSyncPath ?? @"C:\DefaultPath",
            true,
            null,
            null,
            false,
            false,
            5,
            50,
            autoSyncInterval);
}
