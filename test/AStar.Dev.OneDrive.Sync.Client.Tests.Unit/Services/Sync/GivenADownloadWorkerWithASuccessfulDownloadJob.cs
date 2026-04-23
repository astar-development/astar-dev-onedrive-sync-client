using System.Net;
using System.Threading.Channels;
using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public sealed class GivenADownloadWorkerWithASuccessfulDownloadJob : IDisposable
{
    private const string AccessToken = "test-token";
    private const string RelativePath = "Documents/report.docx";
    private const string FileContent = "file content here";

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService = Substitute.For<IGraphService>();
    private readonly HttpDownloader downloader;
    private readonly string localPath;

    public GivenADownloadWorkerWithASuccessfulDownloadJob()
    {
        localPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        downloader = new HttpDownloader(new HttpClient(new FixedResponseHandler(HttpStatusCode.OK, FileContent)));
    }

    public void Dispose()
    {
        downloader.Dispose();
        if(File.Exists(localPath))
            File.Delete(localPath);
    }

    [Fact]
    public async Task when_download_succeeds_then_job_state_is_set_to_in_progress()
    {
        SyncJob job = BuildDownloadJob(localPath, RelativePath);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);

        await sut.RunAsync(SyncChannel.With(job), AccessToken, (_, _, _) => { }, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).UpdateJobStateAsync(job.Id, SyncJobState.InProgress, Arg.Any<string?>());
    }

    [Fact]
    public async Task when_download_succeeds_then_job_state_is_set_to_completed()
    {
        SyncJob job = BuildDownloadJob(localPath, RelativePath);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);

        await sut.RunAsync(SyncChannel.With(job), AccessToken, (_, _, _) => { }, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).UpdateJobStateAsync(job.Id, SyncJobState.Completed, Arg.Any<string?>());
    }

    [Fact]
    public async Task when_download_succeeds_then_on_job_complete_is_called_with_success_true()
    {
        SyncJob job = BuildDownloadJob(localPath, RelativePath);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);
        var capturedSuccess = false;

        await sut.RunAsync(SyncChannel.With(job), AccessToken,
            (_, success, _) => capturedSuccess = success,
            TestContext.Current.CancellationToken);

        capturedSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task when_download_succeeds_then_on_job_complete_is_called_with_null_error()
    {
        SyncJob job = BuildDownloadJob(localPath, RelativePath);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);
        var capturedError = "sentinel";

        await sut.RunAsync(SyncChannel.With(job), AccessToken,
            (_, _, error) => capturedError = error,
            TestContext.Current.CancellationToken);

        capturedError.ShouldBeNull();
    }

    private static SyncJob BuildDownloadJob(string localFilePath, string relativePath)
        => new()
        {
            Direction    = SyncDirection.Download,
            DownloadUrl  = "http://localhost/file.bin",
            LocalPath    = localFilePath,
            RelativePath = relativePath
        };
}

public sealed class GivenADownloadWorkerWithANullDownloadUrl : IDisposable
{
    private const string AccessToken = "test-token";

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService = Substitute.For<IGraphService>();
    private readonly HttpDownloader downloader = new();

    public void Dispose() => downloader.Dispose();

    [Fact]
    public async Task when_download_url_is_null_then_job_state_is_set_to_failed()
    {
        SyncJob job = BuildNullUrlJob();
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);

        await sut.RunAsync(SyncChannel.With(job), AccessToken, (_, _, _) => { }, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).UpdateJobStateAsync(job.Id, SyncJobState.Failed, Arg.Any<string?>());
    }

    [Fact]
    public async Task when_download_url_is_null_then_on_job_complete_is_called_with_success_false()
    {
        SyncJob job = BuildNullUrlJob();
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);
        var capturedSuccess = true;

        await sut.RunAsync(SyncChannel.With(job), AccessToken,
            (_, success, _) => capturedSuccess = success,
            TestContext.Current.CancellationToken);

        capturedSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task when_download_url_is_null_then_on_job_complete_is_called_with_non_null_error()
    {
        SyncJob job = BuildNullUrlJob();
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);
        string? capturedError = null;

        await sut.RunAsync(SyncChannel.With(job), AccessToken,
            (_, _, error) => capturedError = error,
            TestContext.Current.CancellationToken);

        _ = capturedError.ShouldNotBeNull();
    }

    private static SyncJob BuildNullUrlJob()
        => new()
        {
            Direction    = SyncDirection.Download,
            DownloadUrl  = null,
            LocalPath    = "/tmp/irrelevant.bin",
            RelativePath = "irrelevant.bin"
        };
}

public sealed class GivenADownloadWorkerWithAnHttpErrorDuringDownload : IDisposable
{
    private const string AccessToken = "test-token";

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService = Substitute.For<IGraphService>();
    private readonly HttpDownloader downloader;
    private readonly string localPath;

    public GivenADownloadWorkerWithAnHttpErrorDuringDownload()
    {
        localPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        downloader = new HttpDownloader(new HttpClient(new FixedResponseHandler(HttpStatusCode.InternalServerError)), maxRetries: 0);
    }

    public void Dispose()
    {
        downloader.Dispose();
        if(File.Exists(localPath))
            File.Delete(localPath);
    }

    [Fact]
    public async Task when_http_server_returns_500_then_job_state_is_set_to_failed()
    {
        SyncJob job = BuildFailingDownloadJob(localPath);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);

        await sut.RunAsync(SyncChannel.With(job), AccessToken, (_, _, _) => { }, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).UpdateJobStateAsync(job.Id, SyncJobState.Failed, Arg.Any<string?>());
    }

    [Fact]
    public async Task when_http_server_returns_500_then_on_job_complete_is_called_with_success_false()
    {
        SyncJob job = BuildFailingDownloadJob(localPath);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);
        var capturedSuccess = true;

        await sut.RunAsync(SyncChannel.With(job), AccessToken,
            (_, success, _) => capturedSuccess = success,
            TestContext.Current.CancellationToken);

        capturedSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task when_http_server_returns_500_then_on_job_complete_is_called_with_non_null_error()
    {
        SyncJob job = BuildFailingDownloadJob(localPath);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);
        string? capturedError = null;

        await sut.RunAsync(SyncChannel.With(job), AccessToken,
            (_, _, error) => capturedError = error,
            TestContext.Current.CancellationToken);

        _ = capturedError.ShouldNotBeNull();
    }

    private static SyncJob BuildFailingDownloadJob(string localFilePath)
        => new()
        {
            Direction    = SyncDirection.Download,
            DownloadUrl  = "http://localhost/bad-file.bin",
            LocalPath    = localFilePath,
            RelativePath = "bad-file.bin"
        };
}

public sealed class GivenADownloadWorkerWithASuccessfulUploadJob : IDisposable
{
    private const string AccessToken = "test-token";
    private const string RemoteItemId = "remote-item-id";
    private const string FolderId = "folder-123";

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService = Substitute.For<IGraphService>();
    private readonly HttpDownloader downloader = new();
    private readonly string localPath;

    public GivenADownloadWorkerWithASuccessfulUploadJob()
    {
        localPath = Path.GetTempFileName();
        File.WriteAllText(localPath, "upload content");
        _ = graphService.UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(RemoteItemId);
    }

    public void Dispose()
    {
        downloader.Dispose();
        if(File.Exists(localPath))
            File.Delete(localPath);
    }

    [Fact]
    public async Task when_upload_succeeds_then_job_state_is_set_to_in_progress()
    {
        SyncJob job = BuildUploadJob(localPath, FolderId);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);

        await sut.RunAsync(SyncChannel.With(job), AccessToken, (_, _, _) => { }, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).UpdateJobStateAsync(job.Id, SyncJobState.InProgress, Arg.Any<string?>());
    }

    [Fact]
    public async Task when_upload_succeeds_then_job_state_is_set_to_completed()
    {
        SyncJob job = BuildUploadJob(localPath, FolderId);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);

        await sut.RunAsync(SyncChannel.With(job), AccessToken, (_, _, _) => { }, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).UpdateJobStateAsync(job.Id, SyncJobState.Completed, Arg.Any<string?>());
    }

    [Fact]
    public async Task when_upload_succeeds_then_graph_service_is_called_with_access_token_and_folder_id()
    {
        SyncJob job = BuildUploadJob(localPath, FolderId);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);

        await sut.RunAsync(SyncChannel.With(job), AccessToken, (_, _, _) => { }, TestContext.Current.CancellationToken);

        _ = await graphService.Received(1).UploadFileAsync(AccessToken, localPath, Arg.Any<string>(), FolderId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_upload_succeeds_then_on_job_complete_is_called_with_success_true()
    {
        SyncJob job = BuildUploadJob(localPath, FolderId);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);
        var capturedSuccess = false;

        await sut.RunAsync(SyncChannel.With(job), AccessToken,
            (_, success, _) => capturedSuccess = success,
            TestContext.Current.CancellationToken);

        capturedSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task when_upload_succeeds_then_on_job_complete_is_called_with_null_error()
    {
        SyncJob job = BuildUploadJob(localPath, FolderId);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);
        var capturedError = "sentinel";

        await sut.RunAsync(SyncChannel.With(job), AccessToken,
            (_, _, error) => capturedError = error,
            TestContext.Current.CancellationToken);

        capturedError.ShouldBeNull();
    }

    [Fact]
    public async Task when_upload_job_has_download_url_then_upload_uses_download_url_as_remote_path()
    {
        const string DownloadUrl = "https://example.com/remote/path/to/file.txt";
        var job = new SyncJob
        {
            Direction    = SyncDirection.Upload,
            LocalPath    = localPath,
            RelativePath = "fallback/path.txt",
            DownloadUrl  = DownloadUrl,
            FolderId     = FolderId
        };

        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);

        await sut.RunAsync(SyncChannel.With(job), AccessToken, (_, _, _) => { }, TestContext.Current.CancellationToken);

        _ = await graphService.Received(1).UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            DownloadUrl,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_upload_job_has_no_download_url_then_upload_uses_relative_path_as_remote_path()
    {
        const string UploadRelativePath = "Documents/no-url-file.txt";
        var job = new SyncJob
        {
            Direction    = SyncDirection.Upload,
            LocalPath    = localPath,
            RelativePath = UploadRelativePath,
            DownloadUrl  = null,
            FolderId     = FolderId
        };

        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);

        await sut.RunAsync(SyncChannel.With(job), AccessToken, (_, _, _) => { }, TestContext.Current.CancellationToken);

        _ = await graphService.Received(1).UploadFileAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            UploadRelativePath,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    private static SyncJob BuildUploadJob(string localFilePath, string folderId)
        => new()
        {
            Direction    = SyncDirection.Upload,
            LocalPath    = localFilePath,
            RelativePath = "Documents/upload.txt",
            FolderId     = folderId
        };
}

public sealed class GivenADownloadWorkerWithAFailingUploadJob : IDisposable
{
    private const string AccessToken = "test-token";
    private const string UploadErrorMessage = "Graph API unavailable";

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService = Substitute.For<IGraphService>();
    private readonly HttpDownloader downloader = new();
    private readonly string localPath;

    public GivenADownloadWorkerWithAFailingUploadJob()
    {
        localPath = Path.GetTempFileName();
        File.WriteAllText(localPath, "upload content");
        _ = graphService.UploadFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                    .Returns(Task.FromException<string>(new HttpRequestException(UploadErrorMessage)));
    }

    public void Dispose()
    {
        downloader.Dispose();
        if(File.Exists(localPath))
            File.Delete(localPath);
    }

    [Fact]
    public async Task when_upload_throws_then_job_state_is_set_to_failed_with_error_message()
    {
        SyncJob job = BuildFailingUploadJob(localPath);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);

        await sut.RunAsync(SyncChannel.With(job), AccessToken, (_, _, _) => { }, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).UpdateJobStateAsync(job.Id, SyncJobState.Failed, UploadErrorMessage);
    }

    [Fact]
    public async Task when_upload_throws_then_on_job_complete_is_called_with_success_false()
    {
        SyncJob job = BuildFailingUploadJob(localPath);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);
        var capturedSuccess = true;

        await sut.RunAsync(SyncChannel.With(job), AccessToken,
            (_, success, _) => capturedSuccess = success,
            TestContext.Current.CancellationToken);

        capturedSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task when_upload_throws_then_on_job_complete_is_called_with_the_exception_message()
    {
        SyncJob job = BuildFailingUploadJob(localPath);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);
        string? capturedError = null;

        await sut.RunAsync(SyncChannel.With(job), AccessToken,
            (_, _, error) => capturedError = error,
            TestContext.Current.CancellationToken);

        capturedError.ShouldBe(UploadErrorMessage);
    }

    private static SyncJob BuildFailingUploadJob(string localFilePath)
        => new()
        {
            Direction    = SyncDirection.Upload,
            LocalPath    = localFilePath,
            RelativePath = "Documents/upload.txt",
            FolderId     = "folder-err"
        };
}

public sealed class GivenADownloadWorkerWithADeleteJobForAnExistingFile : IDisposable
{
    private const string AccessToken = "test-token";

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService = Substitute.For<IGraphService>();
    private readonly HttpDownloader downloader = new();
    private readonly string localPath;

    public GivenADownloadWorkerWithADeleteJobForAnExistingFile()
    {
        localPath = Path.GetTempFileName();
        File.WriteAllText(localPath, "data to be deleted");
    }

    public void Dispose()
    {
        downloader.Dispose();
        if(File.Exists(localPath))
            File.Delete(localPath);
    }

    [Fact]
    public async Task when_delete_runs_on_existing_file_then_file_is_removed_from_disk()
    {
        SyncJob job = BuildDeleteJob(localPath);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);

        await sut.RunAsync(SyncChannel.With(job), AccessToken, (_, _, _) => { }, TestContext.Current.CancellationToken);

        File.Exists(localPath).ShouldBeFalse();
    }

    [Fact]
    public async Task when_delete_runs_on_existing_file_then_job_state_is_set_to_completed()
    {
        SyncJob job = BuildDeleteJob(localPath);
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);

        await sut.RunAsync(SyncChannel.With(job), AccessToken, (_, _, _) => { }, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).UpdateJobStateAsync(job.Id, SyncJobState.Completed, Arg.Any<string?>());
    }

    private static SyncJob BuildDeleteJob(string localFilePath)
        => new()
        {
            Direction    = SyncDirection.Delete,
            LocalPath    = localFilePath,
            RelativePath = Path.GetFileName(localFilePath)
        };
}

public sealed class GivenADownloadWorkerWithADeleteJobForANonExistentFile : IDisposable
{
    private const string AccessToken = "test-token";

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService = Substitute.For<IGraphService>();
    private readonly HttpDownloader downloader = new();

    public void Dispose() => downloader.Dispose();

    [Fact]
    public async Task when_delete_runs_on_nonexistent_file_then_no_exception_is_thrown_and_state_is_completed()
    {
        const string NonExistentPath = "/tmp/this-file-does-not-exist-qx99z.txt";
        var job = new SyncJob
        {
            Direction    = SyncDirection.Delete,
            LocalPath    = NonExistentPath,
            RelativePath = "this-file-does-not-exist-qx99z.txt"
        };

        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);

        await sut.RunAsync(SyncChannel.With(job), AccessToken, (_, _, _) => { }, TestContext.Current.CancellationToken);

        await syncRepository.Received(1).UpdateJobStateAsync(job.Id, SyncJobState.Completed, Arg.Any<string?>());
    }
}

public sealed class GivenADownloadWorkerWhereTheCancellationTokenIsCancelled : IDisposable
{
    private const string AccessToken = "test-token";

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService = Substitute.For<IGraphService>();
    private readonly HttpDownloader downloader = new();

    public void Dispose() => downloader.Dispose();

    [Fact]
    public async Task when_cancellation_is_requested_mid_job_then_operation_cancelled_exception_propagates()
    {
        var job = new SyncJob
        {
            Direction    = SyncDirection.Download,
            DownloadUrl  = "http://localhost/slow-file.bin",
            LocalPath    = "/tmp/slow-file.bin",
            RelativePath = "slow-file.bin"
        };

        var cts = new CancellationTokenSource();
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);
        var channel = Channel.CreateUnbounded<SyncJob>();

        await channel.Writer.WriteAsync(job);

        syncRepository
            .When(repo => repo.UpdateJobStateAsync(job.Id, SyncJobState.InProgress, Arg.Any<string?>()))
            .Do(_ => cts.Cancel());

        _ = await sut.RunAsync(channel.Reader, AccessToken, (_, _, _) => { }, cts.Token)
                 .ShouldThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task when_cancellation_is_requested_mid_job_then_in_flight_job_state_is_reset_to_queued()
    {
        var job = new SyncJob
        {
            Direction    = SyncDirection.Download,
            DownloadUrl  = "http://localhost/slow-file.bin",
            LocalPath    = "/tmp/slow-file.bin",
            RelativePath = "slow-file.bin"
        };

        var cts = new CancellationTokenSource();
        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);
        var channel = Channel.CreateUnbounded<SyncJob>();

        await channel.Writer.WriteAsync(job);

        syncRepository
            .When(repo => repo.UpdateJobStateAsync(job.Id, SyncJobState.InProgress, Arg.Any<string?>()))
            .Do(_ => cts.Cancel());

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => sut.RunAsync(channel.Reader, AccessToken, (_, _, _) => { }, cts.Token));

        await syncRepository.Received(1).UpdateJobStateAsync(job.Id, SyncJobState.Queued, Arg.Any<string?>());
    }
}

public sealed class GivenADownloadWorkerWhereAnExceptionOccursInsideAJob : IDisposable
{
    private const string AccessToken = "test-token";

    private readonly ISyncRepository syncRepository = Substitute.For<ISyncRepository>();
    private readonly IGraphService graphService = Substitute.For<IGraphService>();
    private readonly HttpDownloader downloader = new();

    public void Dispose() => downloader.Dispose();

    [Fact]
    public async Task when_job_execution_throws_then_on_job_complete_callback_is_always_invoked()
    {
        var job = new SyncJob
        {
            Direction    = SyncDirection.Download,
            DownloadUrl  = null,
            LocalPath    = "/tmp/irrelevant.bin",
            RelativePath = "irrelevant.bin"
        };

        var sut = new DownloadWorker(1, downloader, graphService, syncRepository);
        var callbackInvoked = false;

        await sut.RunAsync(SyncChannel.With(job), AccessToken,
            (_, _, _) => callbackInvoked = true,
            TestContext.Current.CancellationToken);

        callbackInvoked.ShouldBeTrue();
    }
}

file static class SyncChannel
{
    internal static ChannelReader<SyncJob> With(SyncJob job)
    {
        var channel = Channel.CreateUnbounded<SyncJob>();
        _ = channel.Writer.TryWrite(job);
        channel.Writer.Complete();

        return channel.Reader;
    }
}

file sealed class FixedResponseHandler(HttpStatusCode statusCode, string body = "") : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(body) });
}
