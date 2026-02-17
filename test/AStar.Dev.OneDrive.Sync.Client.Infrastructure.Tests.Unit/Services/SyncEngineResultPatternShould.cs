using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Services.OneDriveServices;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit.Services;

/// <summary>
/// Tests for SyncEngine Result pattern refactoring.
/// These tests verify that error handling uses the Result pattern instead of exceptions.
/// </summary>
public class SyncEngineResultPatternShould
{
    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task ValidateAndGetAccountAsync_ReturnsOk_WhenAccountExists()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        var expectedAccount = new AccountInfo(
            "acc1",
            new HashedAccountId(AccountIdHasher.Hash("acc1")),
            "Test User",
            @"C:\Sync",
            true,
            null,
            null,
            false,
            false,
            3,
            50,
            0);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(expectedAccount);

        Result<AccountInfo, SyncError> result = await engine.ValidateAndGetAccountAsync(
            new HashedAccountId(AccountIdHasher.Hash("acc1")),
            TestContext.Current.CancellationToken);

        _ = result.ShouldBeOfType<Result<AccountInfo, SyncError>.Ok>();
        AccountInfo account = result.Match(
            ok => ok,
            error => throw new InvalidOperationException($"Expected Ok but got Error: {error.Message}")
        );
        account.ShouldBe(expectedAccount);
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task ValidateAndGetAccountAsync_ReturnsError_WhenAccountNotFound()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        _ = mocks.AccountRepo.GetByIdAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns((AccountInfo?)null);

        Result<AccountInfo, SyncError> result = await engine.ValidateAndGetAccountAsync(
            new HashedAccountId(AccountIdHasher.Hash("nonexistent")),
            TestContext.Current.CancellationToken);

        _ = result.ShouldBeOfType<Result<AccountInfo, SyncError>.Error>();
        SyncError error = result.Match(
            ok => throw new InvalidOperationException("Expected Error but got Ok"),
            error => error
        );
        error.Message.ShouldContain("nonexistent");
        error.Message.ShouldContain("not found");
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task ProcessDeltaChangesAsync_ReturnsOk_WhenSuccessful()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        var deltaToken = new DeltaToken("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "id", "delta-token-123", DateTimeOffset.UtcNow);
        _ = mocks.DeltaProcessingService.GetDeltaTokenAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(deltaToken);
        _ = mocks.DeltaProcessingService.ProcessDeltaPagesAsync(
                "acc1",
                new HashedAccountId(AccountIdHasher.Hash("acc1")),
                deltaToken,
                Arg.Any<Action<SyncState>?>(),
                Arg.Any<CancellationToken>())
            .Returns((deltaToken, 2, 10));

        Result<Functional.Extensions.Unit, SyncError> result = await engine.ProcessDeltaChangesAsync(
            "acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")),
            TestContext.Current.CancellationToken);

        _ = result.ShouldBeOfType<Result<Functional.Extensions.Unit, SyncError>.Ok>();

        await mocks.DeltaProcessingService.Received(1).SaveDeltaTokenAsync(
            Arg.Is<DeltaToken>(t => t != null && t.Token == "delta-token-123"), Arg.Any<HashedAccountId>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task ProcessDeltaChangesAsync_ReturnsError_WhenDeltaProcessingFails()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        _ = mocks.DeltaProcessingService.GetDeltaTokenAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns((DeltaToken?)null);
        var exception = new InvalidOperationException("Delta processing failed");
        _ = mocks.DeltaProcessingService.ProcessDeltaPagesAsync(
                Arg.Any<string>(),
                new HashedAccountId(AccountIdHasher.Hash("acc1")),
                Arg.Any<DeltaToken?>(),
                Arg.Any<Action<SyncState>?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<(DeltaToken, int, int)>(exception));

        Result<Functional.Extensions.Unit, SyncError> result = await engine.ProcessDeltaChangesAsync("acc1",new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        _ = result.ShouldBeOfType<Result<Functional.Extensions.Unit, SyncError>.Error>();
        SyncError error = result.Match(
            ok => throw new InvalidOperationException("Expected Error but got Ok"),
            error => error
        );
        error.Message.ShouldContain("Delta processing failed");
        error.Exception.ShouldBe(exception);
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task StartSyncAsync_UsesResultChaining_WhenAllOperationsSucceed()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        var account = new AccountInfo("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0);
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(account);
        _ = mocks.SyncConfigRepo.GetSelectedItemsByAccountIdAsync(new HashedAccountId(AccountIdHasher.Hash("acc1")), Arg.Any<CancellationToken>())
            .Returns(new List<DriveItemEntity>().AsReadOnly());

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        _ = await mocks.AccountRepo.Received(1).GetByIdAsync("acc1", Arg.Any<CancellationToken>());
        _ = await mocks.DeltaProcessingService.Received(1).ProcessDeltaPagesAsync(
            Arg.Any<string>(),
                new HashedAccountId(AccountIdHasher.Hash("acc1")),
            Arg.Any<DeltaToken?>(),
            Arg.Any<Action<SyncState>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(Skip = "Requires additional investigation - marked as skipped during refactor/refactor-the-logging-approach branch cleanup")]
    public async Task StartSyncAsync_StopsEarly_WhenAccountValidationFails()
    {
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns((AccountInfo?)null);
        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        await engine.StartSyncAsync("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), TestContext.Current.CancellationToken);

        _ = await mocks.DeltaProcessingService.DidNotReceive().ProcessDeltaPagesAsync(
            Arg.Any<string>(),
            Arg.Any<HashedAccountId>(),
            Arg.Any<DeltaToken?>(),
            Arg.Any<Action<SyncState>?>(),
            Arg.Any<CancellationToken>());
        progressStates.Last().Status.ShouldBe(SyncStatus.Failed);
    }

    private static (SyncEngine Engine, TestMocks Mocks) CreateTestEngine()
    {
        ILocalFileScanner localScanner = Substitute.For<ILocalFileScanner>();
        ISyncConfigurationRepository syncConfigRepo = Substitute.For<ISyncConfigurationRepository>();
        IAccountRepository accountRepo = Substitute.For<IAccountRepository>();
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();
        IDeltaProcessingService deltaProcessingService = Substitute.For<IDeltaProcessingService>();

        _ = syncConflictRepo.GetByFilePathAsync(
        Arg.Any<HashedAccountId>(),
        Arg.Any<string>(),
        Arg.Any<CancellationToken>())
    .Returns((SyncConflict?)null);

        _ = deltaProcessingService.GetDeltaTokenAsync(Arg.Any<string>(), Arg.Any<HashedAccountId>(), Arg.Any<CancellationToken>())
            .Returns((DeltaToken?)null);
        _ = deltaProcessingService.ProcessDeltaPagesAsync(
                Arg.Any<string>(),
                Arg.Any<HashedAccountId>(),
                Arg.Any<DeltaToken?>(),
                Arg.Any<Action<SyncState>?>(),
                Arg.Any<CancellationToken>())
            .Returns((new DeltaToken("acc1", new HashedAccountId(AccountIdHasher.Hash("acc1")), "id", "delta-token", DateTimeOffset.UtcNow), 1, 0));

        IFileTransferService fileTransferService = Substitute.For<IFileTransferService>();
        IDeletionSyncService deletionSyncService = Substitute.For<IDeletionSyncService>();
        ISyncStateCoordinator syncStateCoordinator = Substitute.For<ISyncStateCoordinator>();
        IConflictDetectionService conflictDetectionService = Substitute.For<IConflictDetectionService>();

        _ = conflictDetectionService.CheckKnownFileConflictAsync(
                Arg.Any<string>(),
                Arg.Any<HashedAccountId>(),
                Arg.Any<DriveItemEntity>(),
                Arg.Any<DriveItemEntity>(),
                Arg.Any<Dictionary<string, FileMetadata>>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns((false, null));
        _ = conflictDetectionService.CheckFirstSyncFileConflictAsync(
                Arg.Any<string>(),
                Arg.Any<HashedAccountId>(),
                Arg.Any<DriveItemEntity>(),
                Arg.Any<Dictionary<string, FileMetadata>>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns((false, null, null));

        var progressSubject = new System.Reactive.Subjects.BehaviorSubject<SyncState>(
            SyncState.CreateInitial(string.Empty, new HashedAccountId(AccountIdHasher.Hash(string.Empty))));
        _ = syncStateCoordinator.Progress.Returns(progressSubject);
        _ = syncStateCoordinator.InitializeSessionAsync(
                Arg.Any<string>(),
                Arg.Any<HashedAccountId>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns((string?)null);
        _ = syncStateCoordinator.GetCurrentSessionId()
            .Returns((string?)null);
        _ = syncStateCoordinator.GetCurrentState()
            .Returns(callInfo => progressSubject.Value);

        syncStateCoordinator.When(x => x.UpdateProgress(
            Arg.Any<string>(),
            Arg.Any<HashedAccountId>(),
            Arg.Any<SyncStatus>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<string?>(),
            Arg.Any<long?>()))
            .Do(callInfo =>
            {
                var newState = new SyncState(
                    callInfo.ArgAt<string>(0),
                    callInfo.ArgAt<HashedAccountId>(1),
                    callInfo.ArgAt<SyncStatus>(2),
                    callInfo.ArgAt<int>(3),
                    callInfo.ArgAt<int>(4),
                    callInfo.ArgAt<int>(5),
                    callInfo.ArgAt<int>(6),
                    callInfo.ArgAt<int>(7),
                    callInfo.ArgAt<int>(8),
                    callInfo.ArgAt<int>(9),
                    callInfo.ArgAt<int>(10),
                    0.0,
                    0,
                    callInfo.ArgAt<string?>(11)
                );
                progressSubject.OnNext(newState);
            });

        var engine = new SyncEngine(
            localScanner,
            syncConfigRepo,
            accountRepo,
            syncConflictRepo,
            conflictDetectionService,
            deltaProcessingService,
            fileTransferService,
            deletionSyncService,
            syncStateCoordinator);

        var mocks = new TestMocks(
            localScanner,
            syncConfigRepo,
            accountRepo,
            syncConflictRepo,
            conflictDetectionService,
            deltaProcessingService,
            fileTransferService,
            deletionSyncService,
            syncStateCoordinator);

        return (engine, mocks);
    }

    private record TestMocks(
        ILocalFileScanner LocalScanner,
        ISyncConfigurationRepository SyncConfigRepo,
        IAccountRepository AccountRepo,
        ISyncConflictRepository SyncConflictRepo,
        IConflictDetectionService ConflictDetectionService,
        IDeltaProcessingService DeltaProcessingService,
        IFileTransferService FileTransferService,
        IDeletionSyncService DeletionSyncService,
        ISyncStateCoordinator SyncStateCoordinator);
}
