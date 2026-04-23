using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;
using AStar.Dev.OneDrive.Sync.Client.ViewModels;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public sealed class GivenAParallelDownloadPipelineWithAnEmptyJobList : IDisposable
{
    private const string AccessToken = "test-token";
    private const string AccountId   = "account-1";
    private const string FolderId    = "folder-1";

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService      = Substitute.For<IGraphService>();
    private readonly ParallelDownloadPipeline sut;

    public GivenAParallelDownloadPipelineWithAnEmptyJobList()
        => sut = new ParallelDownloadPipeline(syncRepository, graphService, workerCount: 1);

    public void Dispose() => sut.Dispose();

    [Fact]
    public async Task when_run_with_no_jobs_then_on_progress_is_never_called()
    {
        var progressCallCount = 0;

        await sut.RunAsync([], AccessToken, _ => progressCallCount++, _ => { }, AccountId, FolderId, TestContext.Current.CancellationToken);

        progressCallCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_run_with_no_jobs_then_clear_completed_jobs_is_never_called()
    {
        await sut.RunAsync([], AccessToken, _ => { }, _ => { }, AccountId, FolderId, TestContext.Current.CancellationToken);

        await syncRepository.DidNotReceive().ClearCompletedJobsAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task when_run_with_no_jobs_then_on_job_completed_is_never_called()
    {
        var jobCompletedCallCount = 0;

        await sut.RunAsync([], AccessToken, _ => { }, _ => jobCompletedCallCount++, AccountId, FolderId, TestContext.Current.CancellationToken);

        jobCompletedCallCount.ShouldBe(0);
    }
}

public sealed class GivenAParallelDownloadPipelineWithASingleSuccessfulUploadJob : IDisposable
{
    private const string AccessToken   = "test-token";
    private const string AccountId     = "account-1";
    private const string FolderId      = "folder-1";
    private const string RemoteItemId  = "remote-item-xyz";
    private const string RelativePath  = "Documents/report.docx";

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService      = Substitute.For<IGraphService>();
    private readonly ParallelDownloadPipeline sut;
    private readonly string localPath;

    public GivenAParallelDownloadPipelineWithASingleSuccessfulUploadJob()
    {
        localPath = Path.GetTempFileName();
        File.WriteAllText(localPath, "file content");

        _ = graphService
            .UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(RemoteItemId);

        sut = new ParallelDownloadPipeline(syncRepository, graphService, workerCount: 1);
    }

    public void Dispose()
    {
        sut.Dispose();
        if(File.Exists(localPath))
            File.Delete(localPath);
    }

    [Fact]
    public async Task when_single_job_succeeds_then_on_job_completed_is_called_exactly_once()
    {
        var callCount = 0;
        SyncJob job = BuildUploadJob(localPath, RelativePath);

        await sut.RunAsync([job], AccessToken, _ => { }, _ => callCount++, AccountId, FolderId, TestContext.Current.CancellationToken);

        callCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_single_job_succeeds_then_completed_job_state_is_completed()
    {
        SyncJobState? capturedState = null;
        SyncJob job = BuildUploadJob(localPath, RelativePath);

        await sut.RunAsync([job], AccessToken, _ => { }, args => capturedState = args.Job.State, AccountId, FolderId, TestContext.Current.CancellationToken);

        capturedState.ShouldBe(SyncJobState.Completed);
    }

    [Fact]
    public async Task when_single_job_succeeds_then_clear_completed_jobs_is_called_once_with_account_id()
    {
        SyncJob job = BuildUploadJob(localPath, RelativePath);

        await sut.RunAsync([job], AccessToken, _ => { }, _ => { }, AccountId, FolderId, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).ClearCompletedJobsAsync(AccountId);
    }

    [Fact]
    public async Task when_single_job_succeeds_then_on_progress_is_called_at_least_once()
    {
        var progressCallCount = 0;
        SyncJob job = BuildUploadJob(localPath, RelativePath);

        await sut.RunAsync([job], AccessToken, _ => progressCallCount++, _ => { }, AccountId, FolderId, TestContext.Current.CancellationToken);

        progressCallCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task when_single_job_succeeds_then_final_on_progress_has_sync_state_idle()
    {
        var capturedEvents = new List<SyncProgressEventArgs>();
        SyncJob job = BuildUploadJob(localPath, RelativePath);

        await sut.RunAsync([job], AccessToken, capturedEvents.Add, _ => { }, AccountId, FolderId, TestContext.Current.CancellationToken);

        capturedEvents[^1].SyncState.ShouldBe(SyncState.Idle);
    }

    private static SyncJob BuildUploadJob(string localFilePath, string relativePath)
        => new()
        {
            Direction    = SyncDirection.Upload,
            LocalPath    = localFilePath,
            RelativePath = relativePath,
            FolderId     = FolderId
        };
}

public sealed class GivenAParallelDownloadPipelineWithMultipleSuccessfulUploadJobs : IDisposable
{
    private const string AccessToken  = "test-token";
    private const string AccountId    = "account-multi";
    private const string FolderId     = "folder-multi";
    private const string RemoteItemId = "remote-item-multi";
    private const int    JobCount     = 3;

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService      = Substitute.For<IGraphService>();
    private readonly ParallelDownloadPipeline sut;
    private readonly List<string> localPaths        = [];

    public GivenAParallelDownloadPipelineWithMultipleSuccessfulUploadJobs()
    {
        for(var index = 0; index < JobCount; index++)
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, $"content-{index}");
            localPaths.Add(path);
        }

        _ = graphService
            .UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(RemoteItemId);

        sut = new ParallelDownloadPipeline(syncRepository, graphService, workerCount: 2);
    }

    public void Dispose()
    {
        sut.Dispose();
        foreach(var path in localPaths.Where(File.Exists))
            File.Delete(path);
    }

    [Fact]
    public async Task when_three_jobs_succeed_then_on_job_completed_is_called_once_per_job()
    {
        var callCount = 0;
        var jobs = BuildUploadJobs();

        await sut.RunAsync(jobs, AccessToken, _ => { }, _ => callCount++, AccountId, FolderId, TestContext.Current.CancellationToken);

        callCount.ShouldBe(JobCount);
    }

    [Fact]
    public async Task when_three_jobs_succeed_then_final_progress_completed_equals_total()
    {
        var capturedEvents = new List<SyncProgressEventArgs>();
        var jobs = BuildUploadJobs();

        await sut.RunAsync(jobs, AccessToken, capturedEvents.Add, _ => { }, AccountId, FolderId, TestContext.Current.CancellationToken);

        var finalEvent = capturedEvents[^1];
        finalEvent.Completed.ShouldBe(finalEvent.Total);
    }

    [Fact]
    public async Task when_three_jobs_succeed_then_final_on_progress_has_sync_state_idle()
    {
        var capturedEvents = new List<SyncProgressEventArgs>();
        var jobs = BuildUploadJobs();

        await sut.RunAsync(jobs, AccessToken, capturedEvents.Add, _ => { }, AccountId, FolderId, TestContext.Current.CancellationToken);

        capturedEvents[^1].SyncState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public async Task when_three_jobs_succeed_then_clear_completed_jobs_is_called_once_with_correct_account_id()
    {
        var jobs = BuildUploadJobs();

        await sut.RunAsync(jobs, AccessToken, _ => { }, _ => { }, AccountId, FolderId, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).ClearCompletedJobsAsync(AccountId);
    }

    private List<SyncJob> BuildUploadJobs()
        => localPaths
            .Select((path, index) => new SyncJob
            {
                Direction    = SyncDirection.Upload,
                LocalPath    = path,
                RelativePath = $"Documents/file-{index}.txt",
                FolderId     = FolderId
            })
            .ToList();
}

public sealed class GivenAParallelDownloadPipelineWithCancellationBeforeFirstJobIsWritten : IDisposable
{
    private const string AccountId  = "account-cancel";
    private const string FolderId   = "folder-cancel";
    private const string AccessToken = "test-token";

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService      = Substitute.For<IGraphService>();
    private readonly ParallelDownloadPipeline sut;

    public GivenAParallelDownloadPipelineWithCancellationBeforeFirstJobIsWritten()
        => sut = new ParallelDownloadPipeline(syncRepository, graphService, workerCount: 1);

    public void Dispose() => sut.Dispose();

    [Fact]
    public async Task when_token_is_already_cancelled_then_run_async_throws_operation_cancelled_exception()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var jobs = new[] { new SyncJob { Direction = SyncDirection.Delete, LocalPath = "/tmp/never.bin", RelativePath = "never.bin" } };

        await Should.ThrowAsync<OperationCanceledException>(
            () => sut.RunAsync(jobs, AccessToken, _ => { }, _ => { }, AccountId, FolderId, cts.Token));
    }

    [Fact]
    public async Task when_token_is_already_cancelled_then_clear_completed_jobs_is_never_called()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var jobs = new[] { new SyncJob { Direction = SyncDirection.Delete, LocalPath = "/tmp/never.bin", RelativePath = "never.bin" } };

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => sut.RunAsync(jobs, AccessToken, _ => { }, _ => { }, AccountId, FolderId, cts.Token));

        await syncRepository.DidNotReceive().ClearCompletedJobsAsync(Arg.Any<string>());
    }
}

public sealed class GivenAParallelDownloadPipelineWithASingleWorker : IDisposable
{
    private const string AccessToken  = "test-token";
    private const string AccountId    = "account-singleworker";
    private const string FolderId     = "folder-singleworker";
    private const string RemoteItemId = "remote-id";
    private const int    JobCount     = 4;

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService      = Substitute.For<IGraphService>();
    private readonly ParallelDownloadPipeline sut;
    private readonly List<string> localPaths        = [];

    public GivenAParallelDownloadPipelineWithASingleWorker()
    {
        for(var index = 0; index < JobCount; index++)
        {
            var path = Path.GetTempFileName();
            File.WriteAllText(path, $"content-{index}");
            localPaths.Add(path);
        }

        _ = graphService
            .UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(RemoteItemId);

        sut = new ParallelDownloadPipeline(syncRepository, graphService, workerCount: 1);
    }

    public void Dispose()
    {
        sut.Dispose();
        foreach(var path in localPaths.Where(File.Exists))
            File.Delete(path);
    }

    [Fact]
    public async Task when_worker_count_is_one_then_all_jobs_are_processed()
    {
        var callCount = 0;
        var jobs = BuildUploadJobs();

        await sut.RunAsync(jobs, AccessToken, _ => { }, _ => callCount++, AccountId, FolderId, TestContext.Current.CancellationToken);

        callCount.ShouldBe(JobCount);
    }

    [Fact]
    public async Task when_worker_count_is_one_then_clear_completed_jobs_is_called_once()
    {
        var jobs = BuildUploadJobs();

        await sut.RunAsync(jobs, AccessToken, _ => { }, _ => { }, AccountId, FolderId, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).ClearCompletedJobsAsync(AccountId);
    }

    private List<SyncJob> BuildUploadJobs()
        => localPaths
            .Select((path, index) => new SyncJob
            {
                Direction    = SyncDirection.Upload,
                LocalPath    = path,
                RelativePath = $"Documents/single-worker-file-{index}.txt",
                FolderId     = FolderId
            })
            .ToList();
}

public sealed class GivenAParallelDownloadPipelineWhereAJobFails : IDisposable
{
    private const string AccessToken = "test-token";
    private const string AccountId   = "account-fail";
    private const string FolderId    = "folder-fail";

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService      = Substitute.For<IGraphService>();
    private readonly ParallelDownloadPipeline sut;

    public GivenAParallelDownloadPipelineWhereAJobFails()
    {
        _ = graphService
            .UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<string>(new HttpRequestException("Graph API unavailable")));

        sut = new ParallelDownloadPipeline(syncRepository, graphService, workerCount: 1);
    }

    public void Dispose() => sut.Dispose();

    [Fact]
    public async Task when_job_fails_then_completed_job_state_is_failed()
    {
        var localPath = Path.GetTempFileName();
        File.WriteAllText(localPath, "content");
        SyncJobState? capturedState = null;
        var job = new SyncJob { Direction = SyncDirection.Upload, LocalPath = localPath, RelativePath = "fail.txt", FolderId = FolderId };

        try
        {
            await sut.RunAsync([job], AccessToken, _ => { }, args => capturedState = args.Job.State, AccountId, FolderId, TestContext.Current.CancellationToken);
        }
        finally
        {
            if(File.Exists(localPath))
                File.Delete(localPath);
        }

        capturedState.ShouldBe(SyncJobState.Failed);
    }

    [Fact]
    public async Task when_job_fails_then_final_on_progress_still_has_sync_state_idle()
    {
        var localPath = Path.GetTempFileName();
        File.WriteAllText(localPath, "content");
        var capturedEvents = new List<SyncProgressEventArgs>();
        var job = new SyncJob { Direction = SyncDirection.Upload, LocalPath = localPath, RelativePath = "fail.txt", FolderId = FolderId };

        try
        {
            await sut.RunAsync([job], AccessToken, capturedEvents.Add, _ => { }, AccountId, FolderId, TestContext.Current.CancellationToken);
        }
        finally
        {
            if(File.Exists(localPath))
                File.Delete(localPath);
        }

        capturedEvents[^1].SyncState.ShouldBe(SyncState.Idle);
    }

    [Fact]
    public async Task when_job_fails_then_clear_completed_jobs_is_still_called()
    {
        var localPath = Path.GetTempFileName();
        File.WriteAllText(localPath, "content");
        var job = new SyncJob { Direction = SyncDirection.Upload, LocalPath = localPath, RelativePath = "fail.txt", FolderId = FolderId };

        try
        {
            await sut.RunAsync([job], AccessToken, _ => { }, _ => { }, AccountId, FolderId, TestContext.Current.CancellationToken);
        }
        finally
        {
            if(File.Exists(localPath))
                File.Delete(localPath);
        }

        await syncRepository.Received(1).ClearCompletedJobsAsync(AccountId);
    }
}
