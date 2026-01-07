using System.Reactive.Linq;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Services;
using AStarOneDriveClient.Services.Sync;
using AStarOneDriveClient.ViewModels;
using Microsoft.Extensions.Logging;

namespace AStarOneDriveClient.Tests.Unit.ViewModels;

public class ConflictResolutionViewModelShould
{
    [Fact]
    public void ThrowArgumentNullExceptionWhenAccountIdIsNull()
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
        IConflictResolver conflictResolver = Substitute.For<IConflictResolver>();
        ILogger<ConflictResolutionViewModel> logger = Substitute.For<ILogger<ConflictResolutionViewModel>>();

        var exception = Record.Exception(() => new ConflictResolutionViewModel(
            null!,
            syncEngine,
            conflictResolver,
            logger));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentExceptionWhenAccountIdIsEmpty()
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
        IConflictResolver conflictResolver = Substitute.For<IConflictResolver>();
        ILogger<ConflictResolutionViewModel> logger = Substitute.For<ILogger<ConflictResolutionViewModel>>();

        var exception = Record.Exception(() => new ConflictResolutionViewModel(
            string.Empty,
            syncEngine,
            conflictResolver,
            logger));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenSyncEngineIsNull()
    {
        IConflictResolver conflictResolver = Substitute.For<IConflictResolver>();
        ILogger<ConflictResolutionViewModel> logger = Substitute.For<ILogger<ConflictResolutionViewModel>>();

        var exception = Record.Exception(() => new ConflictResolutionViewModel(
            "test-account",
            null!,
            conflictResolver,
            logger));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenConflictResolverIsNull()
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
        ILogger<ConflictResolutionViewModel> logger = Substitute.For<ILogger<ConflictResolutionViewModel>>();

        var exception = Record.Exception(() => new ConflictResolutionViewModel(
            "test-account",
            syncEngine,
            null!,
            logger));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenLoggerIsNull()
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
        IConflictResolver conflictResolver = Substitute.For<IConflictResolver>();

        var exception = Record.Exception(() => new ConflictResolutionViewModel(
            "test-account",
            syncEngine,
            conflictResolver,
            null!));

        exception.ShouldNotBeNull();
        exception.ShouldBeOfType<ArgumentNullException>();
    }

    [Fact]
    public async Task LoadConflictsFromSyncEngineOnInitialization()
    {
        var conflicts = new List<SyncConflict>
        {
            CreateTestConflict("file1.txt"),
            CreateTestConflict("file2.txt")
        };

        var (viewModel, syncEngine, _, _) = CreateTestViewModel(conflicts);

        // Wait for LoadConflictsCommand to complete (auto-executed on construction)
        await Task.Delay(100);

        viewModel.Conflicts.Count.ShouldBe(2);
        viewModel.HasConflicts.ShouldBeTrue();
        await syncEngine.Received(1).GetConflictsAsync("test-account", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetStatusMessageWhenConflictsLoaded()
    {
        var conflicts = new List<SyncConflict> { CreateTestConflict("file1.txt") };
        var (viewModel, _, _, _) = CreateTestViewModel(conflicts);

        await Task.Delay(100);

        viewModel.StatusMessage.ShouldContain("1 conflict(s)");
    }

    [Fact]
    public async Task SetStatusMessageWhenNoConflictsFound()
    {
        var (viewModel, _, _, _) = CreateTestViewModel([]);

        await Task.Delay(100);

        viewModel.StatusMessage.ShouldBe("No conflicts detected.");
        viewModel.HasConflicts.ShouldBeFalse();
    }

    [Fact]
    public async Task ResolveConflictsWithSelectedStrategies()
    {
        var conflicts = new List<SyncConflict>
        {
            CreateTestConflict("file1.txt"),
            CreateTestConflict("file2.txt")
        };

        var (viewModel, _, conflictResolver, _) = CreateTestViewModel(conflicts);

        await Task.Delay(100);

        viewModel.Conflicts[0].SelectedStrategy = ConflictResolutionStrategy.KeepLocal;
        viewModel.Conflicts[1].SelectedStrategy = ConflictResolutionStrategy.KeepRemote;

        await viewModel.ResolveAllCommand.Execute().FirstAsync();

        await conflictResolver.Received(2).ResolveAsync(
            Arg.Any<SyncConflict>(),
            Arg.Any<ConflictResolutionStrategy>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SkipConflictsWithNoneStrategy()
    {
        var conflicts = new List<SyncConflict>
        {
            CreateTestConflict("file1.txt"),
            CreateTestConflict("file2.txt")
        };

        var (viewModel, _, conflictResolver, _) = CreateTestViewModel(conflicts);

        await Task.Delay(100);

        viewModel.Conflicts[0].SelectedStrategy = ConflictResolutionStrategy.KeepLocal;
        viewModel.Conflicts[1].SelectedStrategy = ConflictResolutionStrategy.None;

        await viewModel.ResolveAllCommand.Execute().FirstAsync();

        await conflictResolver.Received(1).ResolveAsync(
            Arg.Any<SyncConflict>(),
            Arg.Any<ConflictResolutionStrategy>(),
            Arg.Any<CancellationToken>());

        viewModel.Conflicts.Count.ShouldBe(1);
        viewModel.StatusMessage.ShouldContain("1 conflict(s)");
        viewModel.StatusMessage.ShouldContain("Skipped 1");
    }

    [Fact]
    public async Task RemoveResolvedConflictsFromCollection()
    {
        var conflicts = new List<SyncConflict> { CreateTestConflict("file1.txt") };
        var (viewModel, _, _, _) = CreateTestViewModel(conflicts);

        await Task.Delay(100);

        viewModel.Conflicts[0].SelectedStrategy = ConflictResolutionStrategy.KeepLocal;

        await viewModel.ResolveAllCommand.Execute().FirstAsync();

        viewModel.Conflicts.Count.ShouldBe(0);
        viewModel.HasConflicts.ShouldBeFalse();
    }

    [Fact]
    public async Task SetIsResolvingFlagDuringResolution()
    {
        var conflicts = new List<SyncConflict> { CreateTestConflict("file1.txt") };
        var (viewModel, _, conflictResolver, _) = CreateTestViewModel(conflicts);

        await Task.Delay(100);

        var isResolvingValues = new List<bool>();

        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ConflictResolutionViewModel.IsResolving))
                isResolvingValues.Add(viewModel.IsResolving);
        };

        conflictResolver.ResolveAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictResolutionStrategy>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await Task.Delay(50);
            });

        viewModel.Conflicts[0].SelectedStrategy = ConflictResolutionStrategy.KeepLocal;

        await viewModel.ResolveAllCommand.Execute().FirstAsync();

        isResolvingValues.ShouldContain(true);
        viewModel.IsResolving.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleErrorsDuringConflictLoading()
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
        IConflictResolver conflictResolver = Substitute.For<IConflictResolver>();
        ILogger<ConflictResolutionViewModel> logger = Substitute.For<ILogger<ConflictResolutionViewModel>>();

        syncEngine.GetConflictsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<SyncConflict>>(_ => throw new InvalidOperationException("Database error"));

        var viewModel = new ConflictResolutionViewModel("test-account", syncEngine, conflictResolver, logger);

        await Task.Delay(100);

        viewModel.StatusMessage.ShouldContain("Error loading conflicts");
        viewModel.IsLoading.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleErrorsDuringResolution()
    {
        var conflicts = new List<SyncConflict> { CreateTestConflict("file1.txt") };
        var (viewModel, _, conflictResolver, _) = CreateTestViewModel(conflicts);

        await Task.Delay(100);

        conflictResolver.ResolveAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictResolutionStrategy>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new IOException("File locked"));

        viewModel.Conflicts[0].SelectedStrategy = ConflictResolutionStrategy.KeepLocal;

        await viewModel.ResolveAllCommand.Execute().FirstAsync();

        viewModel.StatusMessage.ShouldContain("Error resolving conflicts");
        viewModel.IsResolving.ShouldBeFalse();
    }

    [Fact]
    public async Task UpdateHasConflictsPropertyWhenCollectionChanges()
    {
        var conflicts = new List<SyncConflict> { CreateTestConflict("file1.txt") };
        var (viewModel, _, _, _) = CreateTestViewModel(conflicts);

        await Task.Delay(100);

        viewModel.HasConflicts.ShouldBeTrue();

        viewModel.Conflicts[0].SelectedStrategy = ConflictResolutionStrategy.KeepLocal;
        await viewModel.ResolveAllCommand.Execute().FirstAsync();

        viewModel.HasConflicts.ShouldBeFalse();
    }

    [Fact]
    public void ExecuteCancelCommandSuccessfully()
    {
        var (viewModel, _, _, _) = CreateTestViewModel([]);

        viewModel.CancelCommand.Execute().Subscribe();

        viewModel.StatusMessage.ShouldContain("cancelled");
    }

    private static SyncConflict CreateTestConflict(string filePath) => new(
        Guid.NewGuid().ToString(),
        "test-account",
        filePath,
        DateTime.UtcNow.AddHours(-1),
        DateTime.UtcNow,
        1024,
        2048,
        DateTime.UtcNow,
        ConflictResolutionStrategy.None,
        false
    );

    private static (ConflictResolutionViewModel ViewModel, ISyncEngine SyncEngine, IConflictResolver ConflictResolver, ILogger<ConflictResolutionViewModel> Logger)
        CreateTestViewModel(List<SyncConflict> conflicts)
    {
        ISyncEngine syncEngine = Substitute.For<ISyncEngine>();
        IConflictResolver conflictResolver = Substitute.For<IConflictResolver>();
        ILogger<ConflictResolutionViewModel> logger = Substitute.For<ILogger<ConflictResolutionViewModel>>();

        syncEngine.GetConflictsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(conflicts);

        conflictResolver.ResolveAsync(Arg.Any<SyncConflict>(), Arg.Any<ConflictResolutionStrategy>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var viewModel = new ConflictResolutionViewModel("test-account", syncEngine, conflictResolver, logger);

        return (viewModel, syncEngine, conflictResolver, logger);
    }
}
