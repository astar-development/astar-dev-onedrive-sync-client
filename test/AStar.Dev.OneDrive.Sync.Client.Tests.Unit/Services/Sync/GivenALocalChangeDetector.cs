using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public sealed class GivenALocalChangeDetectorWithANonExistentPath
{
    [Fact]
    public void when_the_local_folder_path_does_not_exist_then_an_empty_list_is_returned()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges("acc-1", "folder-1", "/no/such/path/xzqy99", "remote", null);

        result.ShouldBeEmpty();
    }
}

public sealed class GivenALocalChangeDetectorWithAnEmptyDirectory : IDisposable
{
    private const string AccountId = "acc-empty";
    private const string FolderId = "folder-empty";
    private const string RemoteFolderPath = "OneDrive/Empty";

    private readonly string localRoot;

    public GivenALocalChangeDetectorWithAnEmptyDirectory()
    {
        localRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(localRoot);
    }

    public void Dispose() => Directory.Delete(localRoot, recursive: true);

    [Fact]
    public void when_the_directory_is_empty_then_an_empty_list_is_returned()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, null);

        result.ShouldBeEmpty();
    }
}

public sealed class GivenALocalChangeDetectorWithASingleFileNewerThanTheCutoff : IDisposable
{
    private const string AccountId = "acc-single";
    private const string FolderId = "folder-single";
    private const string FileName = "report.txt";
    private const string RemoteFolderPath = "OneDrive/Docs";
    private const string FileContent = "hello world";

    private readonly string localRoot;
    private readonly string filePath;
    private readonly DateTimeOffset cutoff;

    public GivenALocalChangeDetectorWithASingleFileNewerThanTheCutoff()
    {
        localRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(localRoot);

        cutoff = DateTimeOffset.UtcNow.AddMinutes(-10);

        filePath = Path.Combine(localRoot, FileName);
        File.WriteAllText(filePath, FileContent);
        File.SetLastWriteTimeUtc(filePath, cutoff.UtcDateTime.AddMinutes(5));
        File.SetCreationTimeUtc(filePath, cutoff.UtcDateTime.AddMinutes(5));
    }

    public void Dispose() => Directory.Delete(localRoot, recursive: true);

    [Fact]
    public void when_a_single_file_is_newer_than_the_cutoff_then_one_job_is_returned()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, cutoff);

        result.Count.ShouldBe(1);
    }

    [Fact]
    public void when_a_single_file_is_newer_than_the_cutoff_then_the_job_account_id_matches()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, cutoff);

        result[0].AccountId.ShouldBe(AccountId);
    }

    [Fact]
    public void when_a_single_file_is_newer_than_the_cutoff_then_the_job_folder_id_matches()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, cutoff);

        result[0].FolderId.ShouldBe(FolderId);
    }

    [Fact]
    public void when_a_single_file_is_newer_than_the_cutoff_then_the_job_direction_is_upload()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, cutoff);

        result[0].Direction.ShouldBe(SyncDirection.Upload);
    }

    [Fact]
    public void when_a_single_file_is_newer_than_the_cutoff_then_the_job_relative_path_is_just_the_file_name()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, cutoff);

        result[0].RelativePath.ShouldBe(FileName);
    }

    [Fact]
    public void when_a_single_file_is_newer_than_the_cutoff_then_the_job_local_path_is_the_full_file_path()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, cutoff);

        result[0].LocalPath.ShouldBe(filePath);
    }

    [Fact]
    public void when_a_single_file_is_newer_than_the_cutoff_then_the_job_file_size_matches()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, cutoff);

        result[0].FileSize.ShouldBe(new FileInfo(filePath).Length);
    }

    [Fact]
    public void when_a_single_file_is_newer_than_the_cutoff_then_the_job_download_url_includes_the_remote_folder_prefix()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, cutoff);

        result[0].DownloadUrl.ShouldBe($"{RemoteFolderPath}/{FileName}");
    }
}

public sealed class GivenALocalChangeDetectorWithASingleFileAtOrBeforeTheCutoff : IDisposable
{
    private const string AccountId = "acc-cutoff";
    private const string FolderId = "folder-cutoff";
    private const string RemoteFolderPath = "OneDrive/Cutoff";

    private readonly string localRoot;
    private readonly DateTimeOffset cutoff;

    public GivenALocalChangeDetectorWithASingleFileAtOrBeforeTheCutoff()
    {
        localRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(localRoot);

        cutoff = DateTimeOffset.UtcNow.AddMinutes(-5);

        var oldFilePath = Path.Combine(localRoot, "old.txt");
        File.WriteAllText(oldFilePath, "old content");
        File.SetLastWriteTimeUtc(oldFilePath, cutoff.UtcDateTime);
        File.SetCreationTimeUtc(oldFilePath, cutoff.UtcDateTime);
    }

    public void Dispose() => Directory.Delete(localRoot, recursive: true);

    [Fact]
    public void when_a_file_write_time_equals_the_cutoff_then_it_is_excluded()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, cutoff);

        result.ShouldBeEmpty();
    }
}

public sealed class GivenALocalChangeDetectorWithAHiddenFile : IDisposable
{
    private const string AccountId = "acc-hidden";
    private const string FolderId = "folder-hidden";
    private const string RemoteFolderPath = "OneDrive/Hidden";

    private readonly string localRoot;

    public GivenALocalChangeDetectorWithAHiddenFile()
    {
        localRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(localRoot);

        File.WriteAllText(Path.Combine(localRoot, ".hidden-file"), "hidden content");
    }

    public void Dispose() => Directory.Delete(localRoot, recursive: true);

    [Fact]
    public void when_a_file_name_starts_with_a_dot_then_it_is_skipped()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, null);

        result.ShouldBeEmpty();
    }
}

public sealed class GivenALocalChangeDetectorWithTempFiles : IDisposable
{
    private const string AccountId = "acc-temp";
    private const string FolderId = "folder-temp";
    private const string RemoteFolderPath = "OneDrive/Temp";

    private readonly string localRoot;

    public GivenALocalChangeDetectorWithTempFiles()
    {
        localRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(localRoot);

        File.WriteAllText(Path.Combine(localRoot, "upload.tmp"), "tmp content");
        File.WriteAllText(Path.Combine(localRoot, "download.temp"), "temp content");
        File.WriteAllText(Path.Combine(localRoot, "inprogress.partial"), "partial content");
    }

    public void Dispose() => Directory.Delete(localRoot, recursive: true);

    [Fact]
    public void when_a_file_has_a_tmp_extension_then_it_is_skipped()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, null);

        result.ShouldNotContain(job => job.LocalPath.EndsWith(".tmp"));
    }

    [Fact]
    public void when_a_file_has_a_temp_extension_then_it_is_skipped()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, null);

        result.ShouldNotContain(job => job.LocalPath.EndsWith(".temp"));
    }

    [Fact]
    public void when_a_file_has_a_partial_extension_then_it_is_skipped()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, null);

        result.ShouldNotContain(job => job.LocalPath.EndsWith(".partial"));
    }

    [Fact]
    public void when_all_files_have_temp_extensions_then_an_empty_list_is_returned()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, null);

        result.ShouldBeEmpty();
    }
}

public sealed class GivenALocalChangeDetectorWithNullSince : IDisposable
{
    private const string AccountId = "acc-nullsince";
    private const string FolderId = "folder-nullsince";
    private const string RemoteFolderPath = "OneDrive/NullSince";

    private readonly string localRoot;

    public GivenALocalChangeDetectorWithNullSince()
    {
        localRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(localRoot);

        File.WriteAllText(Path.Combine(localRoot, "alpha.txt"), "alpha");
        File.WriteAllText(Path.Combine(localRoot, "beta.txt"), "beta");
    }

    public void Dispose() => Directory.Delete(localRoot, recursive: true);

    [Fact]
    public void when_since_is_null_then_all_eligible_files_are_returned()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, null);

        result.Count.ShouldBe(2);
    }
}

public sealed class GivenALocalChangeDetectorWithAFileInASubdirectory : IDisposable
{
    private const string AccountId = "acc-subdir";
    private const string FolderId = "folder-subdir";
    private const string SubdirName = "Documents";
    private const string FileName = "notes.txt";
    private const string RemoteFolderPath = "OneDrive/Root";

    private readonly string localRoot;
    private readonly DateTimeOffset cutoff;

    public GivenALocalChangeDetectorWithAFileInASubdirectory()
    {
        localRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(localRoot);

        cutoff = DateTimeOffset.UtcNow.AddMinutes(-10);

        var subDir = Path.Combine(localRoot, SubdirName);
        Directory.CreateDirectory(subDir);

        var filePath = Path.Combine(subDir, FileName);
        File.WriteAllText(filePath, "notes content");
        File.SetLastWriteTimeUtc(filePath, cutoff.UtcDateTime.AddMinutes(5));
        File.SetCreationTimeUtc(filePath, cutoff.UtcDateTime.AddMinutes(5));
    }

    public void Dispose() => Directory.Delete(localRoot, recursive: true);

    [Fact]
    public void when_a_file_is_in_a_subdirectory_then_the_relative_path_uses_forward_slashes()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, cutoff);

        result.ShouldContain(job => job.RelativePath == $"{SubdirName}/{FileName}");
    }

    [Fact]
    public void when_a_file_is_in_a_subdirectory_then_the_download_url_includes_the_remote_folder_prefix()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, cutoff);

        result.ShouldContain(job => job.DownloadUrl == $"{RemoteFolderPath}/{SubdirName}/{FileName}");
    }
}

public sealed class GivenALocalChangeDetectorWithAnEmptyRemoteFolderPath : IDisposable
{
    private const string AccountId = "acc-noremote";
    private const string FolderId = "folder-noremote";
    private const string FileName = "standalone.txt";

    private readonly string localRoot;
    private readonly DateTimeOffset cutoff;

    public GivenALocalChangeDetectorWithAnEmptyRemoteFolderPath()
    {
        localRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(localRoot);

        cutoff = DateTimeOffset.UtcNow.AddMinutes(-10);

        var filePath = Path.Combine(localRoot, FileName);
        File.WriteAllText(filePath, "standalone content");
        File.SetLastWriteTimeUtc(filePath, cutoff.UtcDateTime.AddMinutes(5));
        File.SetCreationTimeUtc(filePath, cutoff.UtcDateTime.AddMinutes(5));
    }

    public void Dispose() => Directory.Delete(localRoot, recursive: true);

    [Fact]
    public void when_remote_folder_path_is_empty_then_download_url_equals_the_relative_path()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, string.Empty, cutoff);

        result[0].DownloadUrl.ShouldBe(FileName);
    }

    [Fact]
    public void when_remote_folder_path_is_empty_then_download_url_does_not_have_a_leading_slash()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, string.Empty, cutoff);

        result[0].DownloadUrl.ShouldNotStartWith("/");
    }
}

public sealed class GivenALocalChangeDetectorWithAHiddenSubdirectory : IDisposable
{
    private const string AccountId = "acc-hiddendir";
    private const string FolderId = "folder-hiddendir";
    private const string RemoteFolderPath = "OneDrive/HiddenDir";
    private const string HiddenDirName = ".git";
    private const string HiddenFileInDir = "config";

    private readonly string localRoot;

    public GivenALocalChangeDetectorWithAHiddenSubdirectory()
    {
        localRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(localRoot);

        var hiddenDir = Path.Combine(localRoot, HiddenDirName);
        Directory.CreateDirectory(hiddenDir);
        File.WriteAllText(Path.Combine(hiddenDir, HiddenFileInDir), "git config content");
    }

    public void Dispose() => Directory.Delete(localRoot, recursive: true);

    [Fact]
    public void when_a_subdirectory_name_starts_with_a_dot_then_its_file_is_not_returned_a_second_time_via_subdirectory_recursion()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, null);

        result.Count(job => job.RelativePath == $"{HiddenDirName}/{HiddenFileInDir}").ShouldBe(1);
    }
}

public sealed class GivenALocalChangeDetectorWithMixedFiles : IDisposable
{
    private const string AccountId = "acc-mixed";
    private const string FolderId = "folder-mixed";
    private const string RemoteFolderPath = "OneDrive/Mixed";

    private readonly string localRoot;
    private readonly DateTimeOffset cutoff;

    public GivenALocalChangeDetectorWithMixedFiles()
    {
        localRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(localRoot);

        cutoff = DateTimeOffset.UtcNow.AddMinutes(-10);

        var eligiblePath = Path.Combine(localRoot, "eligible.txt");
        File.WriteAllText(eligiblePath, "eligible content");
        File.SetLastWriteTimeUtc(eligiblePath, cutoff.UtcDateTime.AddMinutes(5));
        File.SetCreationTimeUtc(eligiblePath, cutoff.UtcDateTime.AddMinutes(5));

        var oldFilePath = Path.Combine(localRoot, "old.txt");
        File.WriteAllText(oldFilePath, "old content");
        File.SetLastWriteTimeUtc(oldFilePath, cutoff.UtcDateTime);
        File.SetCreationTimeUtc(oldFilePath, cutoff.UtcDateTime);

        File.WriteAllText(Path.Combine(localRoot, ".dotfile"), "dot content");
        File.WriteAllText(Path.Combine(localRoot, "upload.tmp"), "tmp content");
    }

    public void Dispose() => Directory.Delete(localRoot, recursive: true);

    [Fact]
    public void when_a_directory_has_mixed_files_then_only_the_eligible_file_is_returned()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, cutoff);

        result.Count.ShouldBe(1);
    }

    [Fact]
    public void when_a_directory_has_mixed_files_then_the_returned_job_is_for_the_eligible_file()
    {
        var sut = new LocalChangeDetector();

        var result = sut.DetectChanges(AccountId, FolderId, localRoot, RemoteFolderPath, cutoff);

        result[0].RelativePath.ShouldBe("eligible.txt");
    }
}
