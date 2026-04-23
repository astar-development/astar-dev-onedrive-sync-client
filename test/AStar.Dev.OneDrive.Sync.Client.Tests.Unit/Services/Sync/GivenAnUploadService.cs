using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public sealed class GivenAnUploadServiceWithANonExistentLocalFile
{
    [Fact]
    public async Task when_local_path_does_not_exist_then_file_not_found_exception_is_thrown()
    {
        var sut = new UploadService();

        _ = await Should.ThrowAsync<FileNotFoundException>(
            () => sut.UploadAsync(null!, "drive-id", "folder-id", "/tmp/does-not-exist-xyz.bin", "remote.bin", ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_local_path_does_not_exist_then_exception_message_contains_the_path()
    {
        const string MissingPath = "/tmp/does-not-exist-xyz.bin";
        var sut = new UploadService();

        var ex = await Should.ThrowAsync<FileNotFoundException>(
            () => sut.UploadAsync(null!, "drive-id", "folder-id", MissingPath, "remote.bin", ct: TestContext.Current.CancellationToken));

        ex.Message.ShouldContain(MissingPath);
    }
}

public sealed class GivenAnUploadServiceWithA201CreatedResponse : IDisposable
{
    private const string ExpectedItemId = "item-abc";
    private const string SuccessJson    = $$$"""{"id":"{{{ExpectedItemId}}}"}""";

    private readonly string localPath;
    private readonly UploadService sut;

    public GivenAnUploadServiceWithA201CreatedResponse()
    {
        localPath = Path.GetTempFileName();
        File.WriteAllText(localPath, "hello world");
        sut = new UploadService(new HttpClient(new FixedPutResponseHandler(HttpStatusCode.Created, SuccessJson)));
    }

    public void Dispose()
    {
        if(File.Exists(localPath))
            File.Delete(localPath);
    }

    [Fact]
    public async Task when_server_responds_with_201_then_upload_returns_item_id()
    {
        var result = await sut.UploadChunksAsync(
            "http://localhost/upload-session", localPath, new FileInfo(localPath).Length, null, TestContext.Current.CancellationToken);

        result.ShouldBe(ExpectedItemId);
    }
}

public sealed class GivenAnUploadServiceWithA200OkResponse : IDisposable
{
    private const string ExpectedItemId = "item-ok";
    private const string SuccessJson    = $$$"""{"id":"{{{ExpectedItemId}}}"}""";

    private readonly string localPath;
    private readonly UploadService sut;

    public GivenAnUploadServiceWithA200OkResponse()
    {
        localPath = Path.GetTempFileName();
        File.WriteAllText(localPath, "hello world");
        sut = new UploadService(new HttpClient(new FixedPutResponseHandler(HttpStatusCode.OK, SuccessJson)));
    }

    public void Dispose()
    {
        if(File.Exists(localPath))
            File.Delete(localPath);
    }

    [Fact]
    public async Task when_server_responds_with_200_then_upload_returns_item_id()
    {
        var result = await sut.UploadChunksAsync(
            "http://localhost/upload-session", localPath, new FileInfo(localPath).Length, null, TestContext.Current.CancellationToken);

        result.ShouldBe(ExpectedItemId);
    }
}

public sealed class GivenAnUploadServiceWhere429ExceedsMaxRetries : IDisposable
{
    private readonly string localPath;
    private readonly UploadService sut;

    public GivenAnUploadServiceWhere429ExceedsMaxRetries()
    {
        localPath = Path.GetTempFileName();
        File.WriteAllText(localPath, "data");
        sut = new UploadService(new HttpClient(new FixedPutResponseHandler(HttpStatusCode.TooManyRequests, "")), maxRetries: 0);
    }

    public void Dispose()
    {
        if(File.Exists(localPath))
            File.Delete(localPath);
    }

    [Fact]
    public async Task when_server_always_returns_429_and_max_retries_exhausted_then_http_request_exception_is_thrown() 
        => _ = await Should.ThrowAsync<HttpRequestException>(
            () => sut.UploadChunksAsync(
                "http://localhost/upload-session", localPath, 1, null, TestContext.Current.CancellationToken));
}

public sealed class GivenAnUploadServiceWhere429OccursOnceThenSucceeds : IDisposable
{
    private const string ExpectedItemId = "item-after-retry";
    private const string SuccessJson    = $$$"""{"id":"{{{ExpectedItemId}}}"}""";

    private readonly string localPath;
    private readonly UploadService sut;

    public GivenAnUploadServiceWhere429OccursOnceThenSucceeds()
    {
        localPath = Path.GetTempFileName();
        File.WriteAllText(localPath, "data");

        var tooManyRequests = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        tooManyRequests.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(0));

        var created = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(SuccessJson)
        };

        sut = new UploadService(new HttpClient(new SequentialPutResponseHandler(tooManyRequests, created)), maxRetries: 1);
    }

    public void Dispose()
    {
        if(File.Exists(localPath))
            File.Delete(localPath);
    }

    [Fact]
    public async Task when_server_returns_429_once_then_201_then_upload_returns_item_id()
    {
        var fileSize = new FileInfo(localPath).Length;
        var result = await sut.UploadChunksAsync(
            "http://localhost/upload-session", localPath, fileSize, null, TestContext.Current.CancellationToken);

        result.ShouldBe(ExpectedItemId);
    }
}

public sealed class GivenAnUploadServiceProgressReporting : IDisposable
{
    private readonly string localPath;
    private readonly UploadService sut;

    public GivenAnUploadServiceProgressReporting()
    {
        localPath = Path.GetTempFileName();
        File.WriteAllText(localPath, "progress test content");
        sut = new UploadService(new HttpClient(new FixedPutResponseHandler(HttpStatusCode.Created, """{"id":"prog-item"}""")));
    }

    public void Dispose()
    {
        if(File.Exists(localPath))
            File.Delete(localPath);
    }

    [Fact]
    public async Task when_upload_completes_then_progress_is_reported_at_least_once()
    {
        var fileSize = new FileInfo(localPath).Length;
        var reports = new List<long>();
        var progress = new Progress<long>(reports.Add);

        _ = await sut.UploadChunksAsync(
            "http://localhost/upload-session", localPath, fileSize, progress, TestContext.Current.CancellationToken);

        reports.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task when_upload_completes_then_final_progress_report_equals_file_size()
    {
        var fileSize = new FileInfo(localPath).Length;
        var reports = new List<long>();
        var progress = new Progress<long>(reports.Add);

        _ = await sut.UploadChunksAsync(
            "http://localhost/upload-session", localPath, fileSize, progress, TestContext.Current.CancellationToken);

        await Task.Delay(50, TestContext.Current.CancellationToken);
        reports.Last().ShouldBe(fileSize);
    }
}

public sealed class GivenUploadServiceBackoffDelayTests
{
    private static MethodInfo GetBackoffMethod() =>
        typeof(UploadService).GetMethod("GetBackoffDelay", BindingFlags.NonPublic | BindingFlags.Static)!;

    [Fact]
    public void when_getting_backoff_for_attempt_1_then_result_is_at_least_base_delay()
    {
        var method = GetBackoffMethod();
        _ = method.ShouldNotBeNull();

        var result = (TimeSpan)method.Invoke(null, [1])!;

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(2.0);
    }

    [Fact]
    public void when_getting_backoff_for_attempt_1_then_result_does_not_exceed_base_plus_jitter()
    {
        var method = GetBackoffMethod();

        var result = (TimeSpan)method.Invoke(null, [1])!;

        result.TotalSeconds.ShouldBeLessThanOrEqualTo(2.4);
    }

    [Fact]
    public void when_getting_backoff_for_attempt_2_then_result_is_at_least_double_base()
    {
        var method = GetBackoffMethod();

        var result = (TimeSpan)method.Invoke(null, [2])!;

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(4.0);
    }

    [Fact]
    public void when_getting_backoff_for_attempt_2_then_result_does_not_exceed_double_base_plus_jitter()
    {
        var method = GetBackoffMethod();

        var result = (TimeSpan)method.Invoke(null, [2])!;

        result.TotalSeconds.ShouldBeLessThanOrEqualTo(4.8);
    }

    [Theory]
    [InlineData(1, 2.0, 2.4)]
    [InlineData(2, 4.0, 4.8)]
    [InlineData(3, 8.0, 9.6)]
    [InlineData(4, 16.0, 19.2)]
    [InlineData(5, 32.0, 38.4)]
    public void when_getting_backoff_for_attempt_then_result_is_within_expected_jitter_bounds(int attempt, double minSeconds, double maxSeconds)
    {
        var method = GetBackoffMethod();

        var result = (TimeSpan)method.Invoke(null, [attempt])!;

        result.TotalSeconds.ShouldBeGreaterThanOrEqualTo(minSeconds);
        result.TotalSeconds.ShouldBeLessThanOrEqualTo(maxSeconds);
    }

    [Fact]
    public void when_getting_backoff_for_a_very_high_attempt_then_result_is_capped_at_max_delay_plus_jitter()
    {
        var method = GetBackoffMethod();

        var result = (TimeSpan)method.Invoke(null, [20])!;

        result.TotalSeconds.ShouldBeLessThanOrEqualTo(144.0);
    }

    [Fact]
    public void when_delays_are_sampled_multiple_times_for_same_attempt_then_jitter_produces_distinct_values()
    {
        var method = GetBackoffMethod();
        var delays = new List<double>();

        for(var i = 0; i < 10; i++)
            delays.Add(((TimeSpan)method.Invoke(null, [1])!).TotalMilliseconds);

        delays.Distinct().Count().ShouldBeGreaterThan(1);
    }

    [Fact]
    public void when_comparing_consecutive_attempt_delays_then_each_is_greater_than_the_previous()
    {
        var method = GetBackoffMethod();
        var delays = Enumerable.Range(1, 5)
            .Select(i => ((TimeSpan)method.Invoke(null, [i])!).TotalSeconds)
            .ToList();

        for(var i = 0; i < delays.Count - 1; i++)
            delays[i].ShouldBeLessThan(delays[i + 1]);
    }
}

file sealed class FixedPutResponseHandler(HttpStatusCode statusCode, string body) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(statusCode) { Content = new StringContent(body) });
}

file sealed class SequentialPutResponseHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> responses;

    internal SequentialPutResponseHandler(params HttpResponseMessage[] ordered)
        => responses = new Queue<HttpResponseMessage>(ordered);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(responses.Count > 0 ? responses.Dequeue() : new HttpResponseMessage(HttpStatusCode.InternalServerError));
}
