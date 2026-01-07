using System.Reactive.Linq;
using System.Reactive.Subjects;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.ViewModels;
using Microsoft.Extensions.Logging;

namespace AStarOneDriveClient.Tests.Unit.ViewModels;

public class SyncProgressViewModelShould
{
    [Fact]
    public void ThrowArgumentExceptionWhenAccountIdIsNull()
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
        ILogger<SyncProgressViewModel> logger = Substitute.For<ILogger<SyncProgressViewModel>>();

        var exception = Record.Exception(() => new SyncProgressViewModel(
            null!,
            syncEngine,
            logger));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentExceptionWhenAccountIdIsEmpty()
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
        ILogger<SyncProgressViewModel> logger = Substitute.For<ILogger<SyncProgressViewModel>>();

        var exception = Record.Exception(() => new SyncProgressViewModel(
            string.Empty,
            syncEngine,
            logger));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenSyncEngineIsNull()
    {
        ILogger<SyncProgressViewModel> logger = Substitute.For<ILogger<SyncProgressViewModel>>();

        var exception = Record.Exception(() => new SyncProgressViewModel(
            "test-account",
            null!,
            logger));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenLoggerIsNull()
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();

        var exception = Record.Exception(() => new SyncProgressViewModel(
            "test-account",
            syncEngine,
            null!));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void InitializeWithDefaultValues()
    {
        var (viewModel, _, _) = CreateTestViewModel();

        viewModel.CurrentProgress.ShouldBeNull();
        viewModel.IsSyncing.ShouldBeFalse();
        viewModel.StatusMessage.ShouldBe("Ready to sync");
        viewModel.ProgressPercentage.ShouldBe(0);
        viewModel.HasConflicts.ShouldBeFalse();
    }

    [Fact]
    public void UpdateProgressWhenProgressObservableEmits()
    {
        var (viewModel, _, progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            5,
            1000,
            500,
            1,
            1,
            0,
            0,
            1.5,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.CurrentProgress.ShouldBe(progress);
        viewModel.ProgressPercentage.ShouldBe(50);
        viewModel.FilesProgressText.ShouldBe("5 of 10 files");
    }

    [Fact]
    public void CalculateProgressPercentageCorrectly()
    {
        var (viewModel, _, progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            100,
            25,
            10000,
            2500,
            5,
            5,
            0,
            0,
            2.5,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.ProgressPercentage.ShouldBe(25);
    }

    [Fact]
    public void DisplayZeroPercentageWhenTotalFilesIsZero()
    {
        var (viewModel, _, progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Idle,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.ProgressPercentage.ShouldBe(0);
    }

    [Fact]
    public void DisplayTransferDetailsCorrectly()
    {
        var (viewModel, _, progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            8,
            10000,
            8000,
            3,
            2,
            0,
            0,
            1.5,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.TransferDetailsText.ShouldContain("uploading");
        viewModel.TransferDetailsText.ShouldContain("downloading");
    }

    [Fact]
    public void DisplayConflictsWhenDetected()
    {
        var (viewModel, _, progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            8,
            10000,
            8000,
            1,
            1,
            0,
            2,
            1.5,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.HasConflicts.ShouldBeTrue();
        viewModel.ConflictsText.ShouldBe("⚠ 2 conflict(s) detected");
    }

    [Fact]
    public void NotDisplayConflictsWhenNoneDetected()
    {
        var (viewModel, _, progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            8,
            10000,
            8000,
            1,
            1,
            0,
            0,
            1.5,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.HasConflicts.ShouldBeFalse();
        viewModel.ConflictsText.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task StartSyncCommandExecutesSuccessfully()
    {
        var (viewModel, syncEngine, _) = CreateTestViewModel();

        syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await viewModel.StartSyncCommand.Execute().FirstAsync();

        await syncEngine.Received(1).StartSyncAsync("test-account", Arg.Any<CancellationToken>());
        viewModel.IsSyncing.ShouldBeFalse();
        viewModel.StatusMessage.ShouldBe("Sync completed successfully");
    }

    [Fact]
    public async Task StartSyncCommandSetsIsSyncingFlag()
    {
        var (viewModel, syncEngine, _) = CreateTestViewModel();
        var isSyncingValues = new List<bool>();

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SyncProgressViewModel.IsSyncing))
                isSyncingValues.Add(viewModel.IsSyncing);
        };

        syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Delay(50);
            });

        await viewModel.StartSyncCommand.Execute().FirstAsync();

        isSyncingValues.ShouldContain(true);
        viewModel.IsSyncing.ShouldBeFalse();
    }

    [Fact]
    public async Task PauseSyncCommandCallsSyncEngine()
    {
        var (viewModel, syncEngine, _) = CreateTestViewModel();

        viewModel.GetType().GetProperty("IsSyncing")!.SetValue(viewModel, true);

        await viewModel.PauseSyncCommand.Execute().FirstAsync();

        await syncEngine.Received(1).StopSyncAsync();
        viewModel.IsSyncing.ShouldBeFalse();
        viewModel.StatusMessage.ShouldBe("Sync paused");
    }

    [Fact]
    public async Task HandleSyncCancellation()
    {
        var (viewModel, syncEngine, _) = CreateTestViewModel();

        syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new OperationCanceledException());

        await viewModel.StartSyncCommand.Execute().FirstAsync();

        viewModel.StatusMessage.ShouldBe("Sync paused");
        viewModel.IsSyncing.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleSyncErrors()
    {
        var (viewModel, syncEngine, _) = CreateTestViewModel();

        syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("Test error"));

        await viewModel.StartSyncCommand.Execute().FirstAsync();

        viewModel.StatusMessage.ShouldContain("Sync failed");
        viewModel.IsSyncing.ShouldBeFalse();
    }

    [Fact]
    public void StartSyncCommandCanExecuteWhenNotSyncing()
    {
        var (viewModel, _, _) = CreateTestViewModel();

        viewModel.GetType().GetProperty("IsSyncing")!.SetValue(viewModel, false);

        viewModel.StartSyncCommand.CanExecute.FirstAsync().Wait().ShouldBeTrue();
    }

    [Fact]
    public void StartSyncCommandCannotExecuteWhenSyncing()
    {
        var (viewModel, _, _) = CreateTestViewModel();

        viewModel.GetType().GetProperty("IsSyncing")!.SetValue(viewModel, true);

        viewModel.StartSyncCommand.CanExecute.FirstAsync().Wait().ShouldBeFalse();
    }

    [Fact]
    public void PauseSyncCommandCanExecuteWhenSyncing()
    {
        var (viewModel, _, _) = CreateTestViewModel();

        viewModel.GetType().GetProperty("IsSyncing")!.SetValue(viewModel, true);

        viewModel.PauseSyncCommand.CanExecute.FirstAsync().Wait().ShouldBeTrue();
    }

    [Fact]
    public void PauseSyncCommandCannotExecuteWhenNotSyncing()
    {
        var (viewModel, _, _) = CreateTestViewModel();

        viewModel.GetType().GetProperty("IsSyncing")!.SetValue(viewModel, false);

        viewModel.PauseSyncCommand.CanExecute.FirstAsync().Wait().ShouldBeFalse();
    }

    [Fact]
    public void ViewConflictsCommandCanExecuteWhenConflictsExist()
    {
        var (viewModel, _, progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            5,
            10000,
            5000,
            1,
            1,
            0,
            2,
            1.5,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.ViewConflictsCommand.CanExecute.FirstAsync().Wait().ShouldBeTrue();
    }

    [Fact]
    public void ViewConflictsCommandCannotExecuteWhenNoConflicts()
    {
        var (viewModel, _, progressSubject) = CreateTestViewModel();

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            5,
            10000,
            5000,
            1,
            1,
            0,
            0,
            1.5,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        viewModel.ViewConflictsCommand.CanExecute.FirstAsync().Wait().ShouldBeFalse();
    }

    [Fact]
    public void RaisePropertyChangedForAllDerivedProperties()
    {
        var (viewModel, _, progressSubject) = CreateTestViewModel();
        var changedProperties = new List<string>();

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is not null)
                changedProperties.Add(e.PropertyName);
        };

        var progress = new SyncState(
            "test-account",
            SyncStatus.Running,
            10,
            5,
            10000,
            5000,
            1,
            1,
            0,
            1,
            1.5,
            null,
            DateTime.UtcNow
        );

        progressSubject.OnNext(progress);

        changedProperties.ShouldContain(nameof(SyncProgressViewModel.CurrentProgress));
        changedProperties.ShouldContain(nameof(SyncProgressViewModel.ProgressPercentage));
        changedProperties.ShouldContain(nameof(SyncProgressViewModel.FilesProgressText));
        changedProperties.ShouldContain(nameof(SyncProgressViewModel.TransferDetailsText));
        changedProperties.ShouldContain(nameof(SyncProgressViewModel.ConflictsText));
        changedProperties.ShouldContain(nameof(SyncProgressViewModel.HasConflicts));
    }

    private static (SyncProgressViewModel ViewModel, ISyncEngine SyncEngine, Subject<SyncState> ProgressSubject)
        CreateTestViewModel()
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
        ILogger<SyncProgressViewModel> logger = Substitute.For<ILogger<SyncProgressViewModel>>();

        var progressSubject = new Subject<SyncState>();
        syncEngine.Progress.Returns(progressSubject);

        syncEngine.StartSyncAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        syncEngine.StopSyncAsync()
            .Returns(Task.CompletedTask);

        var viewModel = new SyncProgressViewModel("test-account", syncEngine, logger);

        return (viewModel, syncEngine, progressSubject);
    }
}
