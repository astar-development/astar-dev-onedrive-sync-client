using AStar.Dev.Functional.Extensions;
using AStar.Dev.OneDrive.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using AStar.Dev.OneDrive.Client.Infrastructure.Services;
using AStar.Dev.OneDrive.Client.Infrastructure.Services.OneDriveServices;
using Unit = AStar.Dev.Functional.Extensions.Unit;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Services;

/// <summary>
/// Tests for SyncEngine Result pattern refactoring.
/// These tests verify that error handling uses the Result pattern instead of exceptions.
/// </summary>
public class SyncEngineResultPatternShould
{
    [Fact]
    public async Task ValidateAndGetAccountAsync_ReturnsOk_WhenAccountExists()
    {
        // Arrange
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        var expectedAccount = new AccountInfo(
            "acc1", 
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

        // Act
        Result<AccountInfo, SyncError> result = await engine.ValidateAndGetAccountAsync(
            "acc1", 
            TestContext.Current.CancellationToken);

        // Assert
        _ = result.ShouldBeOfType<Result<AccountInfo, SyncError>.Ok>();
        
        var account = result.Match(
            ok => ok,
            error => throw new InvalidOperationException($"Expected Ok but got Error: {error.Message}")
        );

        account.ShouldBe(expectedAccount);
    }

    [Fact]
    public async Task ValidateAndGetAccountAsync_ReturnsError_WhenAccountNotFound()
    {
        // Arrange
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        
        _ = mocks.AccountRepo.GetByIdAsync("nonexistent", Arg.Any<CancellationToken>())
            .Returns((AccountInfo?)null);

        // Act
        Result<AccountInfo, SyncError> result = await engine.ValidateAndGetAccountAsync(
            "nonexistent", 
            TestContext.Current.CancellationToken);

        // Assert
        _ = result.ShouldBeOfType<Result<AccountInfo, SyncError>.Error>();
        
        var error = result.Match(
            ok => throw new InvalidOperationException("Expected Error but got Ok"),
            error => error
        );

        error.Message.ShouldContain("nonexistent");
        error.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task ProcessDeltaChangesAsync_ReturnsOk_WhenSuccessful()
    {
        // Arrange
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        var deltaToken = new DeltaToken("acc1", "", "delta-token-123", DateTimeOffset.UtcNow);

        _ = mocks.DeltaProcessingService.GetDeltaTokenAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(deltaToken);

        _ = mocks.DeltaProcessingService.ProcessDeltaPagesAsync(
                "acc1",
                deltaToken,
                Arg.Any<Action<SyncState>?>(),
                Arg.Any<CancellationToken>())
            .Returns((deltaToken, 2, 10));

        // Act
        Result<AStar.Dev.Functional.Extensions.Unit, SyncError> result = await engine.ProcessDeltaChangesAsync(
            "acc1", 
            TestContext.Current.CancellationToken);

        // Assert
        _ = result.ShouldBeOfType<Result<AStar.Dev.Functional.Extensions.Unit, SyncError>.Ok>();
        
        // Verify delta token was saved
        await mocks.DeltaProcessingService.Received(1).SaveDeltaTokenAsync(
            Arg.Is<DeltaToken?>(t => t != null && t.Token == "delta-token-123"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessDeltaChangesAsync_ReturnsError_WhenDeltaProcessingFails()
    {
        // Arrange
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        
        _ = mocks.DeltaProcessingService.GetDeltaTokenAsync("acc1", Arg.Any<CancellationToken>())
            .Returns((DeltaToken?)null);

        var exception = new InvalidOperationException("Delta processing failed");
        _ = mocks.DeltaProcessingService.ProcessDeltaPagesAsync(
                Arg.Any<string>(),
                Arg.Any<DeltaToken?>(),
                Arg.Any<Action<SyncState>?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException<(DeltaToken?, int, int)>(exception));

        // Act
        Result<AStar.Dev.Functional.Extensions.Unit, SyncError> result = await engine.ProcessDeltaChangesAsync(
            "acc1", 
            TestContext.Current.CancellationToken);

        // Assert
        _ = result.ShouldBeOfType<Result<AStar.Dev.Functional.Extensions.Unit, SyncError>.Error>();
        
        var error = result.Match(
            ok => throw new InvalidOperationException("Expected Error but got Ok"),
            error => error
        );

        error.Message.ShouldContain("Delta processing failed");
        error.Exception.ShouldBe(exception);
    }

    [Fact]
    public async Task StartSyncAsync_UsesResultChaining_WhenAllOperationsSucceed()
    {
        // Arrange
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        var account = new AccountInfo("acc1", "Test", @"C:\Sync", true, null, null, false, false, 3, 50, 0);

        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(account);
        
        _ = mocks.SyncConfigRepo.GetSelectedItemsByAccountIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns(new List<DriveItemEntity>().AsReadOnly());

        // Act
        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        // Assert
        // Verify that account validation was called
        await mocks.AccountRepo.Received(1).GetByIdAsync("acc1", Arg.Any<CancellationToken>());
        
        // Verify delta processing was called (even with no selected folders, it should process delta)
        await mocks.DeltaProcessingService.Received(1).ProcessDeltaPagesAsync(
            Arg.Any<string>(),
            Arg.Any<DeltaToken?>(),
            Arg.Any<Action<SyncState>?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartSyncAsync_StopsEarly_WhenAccountValidationFails()
    {
        // Arrange
        (SyncEngine engine, TestMocks mocks) = CreateTestEngine();
        
        _ = mocks.AccountRepo.GetByIdAsync("acc1", Arg.Any<CancellationToken>())
            .Returns((AccountInfo?)null);

        var progressStates = new List<SyncState>();
        _ = engine.Progress.Subscribe(progressStates.Add);

        // Act
        await engine.StartSyncAsync("acc1", TestContext.Current.CancellationToken);

        // Assert
        // Should not proceed to delta processing when account is not found
        await mocks.DeltaProcessingService.DidNotReceive().ProcessDeltaPagesAsync(
            Arg.Any<string>(),
            Arg.Any<DeltaToken?>(),
            Arg.Any<Action<SyncState>?>(),
            Arg.Any<CancellationToken>());
        
        // Status should be Failed
        progressStates.Last().Status.ShouldBe(SyncStatus.Failed);
    }

    private static (SyncEngine Engine, TestMocks Mocks) CreateTestEngine()
    {
        ILocalFileScanner localScanner = Substitute.For<ILocalFileScanner>();
        IRemoteChangeDetector remoteDetector = Substitute.For<IRemoteChangeDetector>();
        IDriveItemsRepository fileMetadataRepo = Substitute.For<IDriveItemsRepository>();
        ISyncConfigurationRepository syncConfigRepo = Substitute.For<ISyncConfigurationRepository>();
        IAccountRepository accountRepo = Substitute.For<IAccountRepository>();
        IGraphApiClient graphApiClient = Substitute.For<IGraphApiClient>();
        ISyncConflictRepository syncConflictRepo = Substitute.For<ISyncConflictRepository>();
        IDeltaProcessingService deltaProcessingService = Substitute.For<IDeltaProcessingService>();

        // Setup default mock return for UploadFileAsync
        _ = graphApiClient.UploadFileAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IProgress<long>?>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(new Microsoft.Graph.Models.DriveItem
            {
                Id = $"uploaded_{Guid.CreateVersion7():N0}",
                Name = callInfo.ArgAt<string>(1).Split('\\', '/').Last(),
                CTag = $"ctag_{Guid.CreateVersion7():N0}",
                ETag = $"etag_{Guid.CreateVersion7():N0}",
                LastModifiedDateTime = DateTimeOffset.UtcNow
            }));

        // Setup default mock for GetByFilePathAsync
        _ = syncConflictRepo.GetByFilePathAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns((SyncConflict?)null);

        // Setup default mock for DeltaProcessingService
        _ = deltaProcessingService.GetDeltaTokenAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((DeltaToken?)null);
        _ = deltaProcessingService.ProcessDeltaPagesAsync(
                Arg.Any<string>(),
                Arg.Any<DeltaToken?>(),
                Arg.Any<Action<SyncState>?>(),
                Arg.Any<CancellationToken>())
            .Returns((new DeltaToken("acc1", "", "delta-token", DateTimeOffset.UtcNow), 1, 0));

        IFileTransferService fileTransferService = Substitute.For<IFileTransferService>();
        IDeletionSyncService deletionSyncService = Substitute.For<IDeletionSyncService>();
        ISyncStateCoordinator syncStateCoordinator = Substitute.For<ISyncStateCoordinator>();
        IConflictDetectionService conflictDetectionService = Substitute.For<IConflictDetectionService>();

        // Setup default mock for ConflictDetectionService
        _ = conflictDetectionService.CheckKnownFileConflictAsync(
                Arg.Any<string>(),
                Arg.Any<DriveItemEntity>(),
                Arg.Any<DriveItemEntity>(),
                Arg.Any<Dictionary<string, FileMetadata>>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns((false, null));
        _ = conflictDetectionService.CheckFirstSyncFileConflictAsync(
                Arg.Any<string>(),
                Arg.Any<DriveItemEntity>(),
                Arg.Any<Dictionary<string, FileMetadata>>(),
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<CancellationToken>())
            .Returns((false, null, null));

        // Setup default mock for SyncStateCoordinator with a BehaviorSubject for Progress
        var progressSubject = new System.Reactive.Subjects.BehaviorSubject<SyncState>(
            SyncState.CreateInitial(string.Empty));
        _ = syncStateCoordinator.Progress.Returns(progressSubject);
        _ = syncStateCoordinator.InitializeSessionAsync(
                Arg.Any<string>(), 
                Arg.Any<bool>(), 
                Arg.Any<CancellationToken>())
            .Returns((string?)null);
        _ = syncStateCoordinator.GetCurrentSessionId()
            .Returns((string?)null);
        _ = syncStateCoordinator.GetCurrentState()
            .Returns(callInfo => progressSubject.Value);

        // Make UpdateProgress actually update the BehaviorSubject
        syncStateCoordinator.When(x => x.UpdateProgress(
            Arg.Any<string>(),
            Arg.Any<SyncStatus>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<long>(),
            Arg.Any<long>(),
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
                    callInfo.ArgAt<SyncStatus>(1),
                    callInfo.ArgAt<int>(2),
                    callInfo.ArgAt<int>(3),
                    callInfo.ArgAt<long>(4),
                    callInfo.ArgAt<long>(5),
                    callInfo.ArgAt<int>(6),
                    callInfo.ArgAt<int>(7),
                    callInfo.ArgAt<int>(8),
                    callInfo.ArgAt<int>(9),
                    0.0, // MegabytesPerSecond - not tracked in UpdateProgress
                    0, // EstimatedSecondsRemaining
                    callInfo.ArgAt<string?>(10) // CurrentStatusMessage from currentScanningFolder param
                );
                progressSubject.OnNext(newState);
            });

        var engine = new SyncEngine(
            localScanner,
            remoteDetector,
            fileMetadataRepo,
            syncConfigRepo,
            accountRepo,
            graphApiClient,
            syncConflictRepo,
            conflictDetectionService,
            deltaProcessingService,
            fileTransferService,
            deletionSyncService,
            syncStateCoordinator);

        var mocks = new TestMocks(
            localScanner,
            remoteDetector,
            fileMetadataRepo,
            syncConfigRepo,
            accountRepo,
            graphApiClient,
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
        IRemoteChangeDetector RemoteDetector,
        IDriveItemsRepository FileMetadataRepo,
        ISyncConfigurationRepository SyncConfigRepo,
        IAccountRepository AccountRepo,
        IGraphApiClient GraphApiClient,
        ISyncConflictRepository SyncConflictRepo,
        IConflictDetectionService ConflictDetectionService,
        IDeltaProcessingService DeltaProcessingService,
        IFileTransferService FileTransferService,
        IDeletionSyncService DeletionSyncService,
        ISyncStateCoordinator SyncStateCoordinator);
}
