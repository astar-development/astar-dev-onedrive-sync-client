using Shouldly;
using AStar.Dev.OneDrive.Sync.Client.Features.LocalChangeDetection.Services;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.LocalChangeDetection.Services;

public class LocalHashServiceShould : IDisposable
{
    private readonly LocalHashService _service;
    private readonly string _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public LocalHashServiceShould()
    {
        _service = new LocalHashService();
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public async Task ComputeFileHashReturnsValidHashForExistingFile()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(testFile, "Hello, World!");

        string? hash = await _service.ComputeFileHashAsync(testFile);

        hash.ShouldNotBeNull();
        hash.Length.ShouldBe(64);
        hash.ShouldMatch(@"^[a-f0-9]{64}$");
    }

    [Fact]
    public async Task ComputeFileHashReturnsNullForNonExistentFile()
    {
        string nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.txt");

        string? hash = await _service.ComputeFileHashAsync(nonExistentFile);

        hash.ShouldBeNull();
    }

    [Fact]
    public async Task ComputeFileHashThrowsArgumentNullExceptionForNullFilePath()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.ComputeFileHashAsync(null!)
        );
    }

    [Fact]
    public async Task ComputeFileHashThrowsArgumentExceptionForEmptyFilePath()
    {
        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ComputeFileHashAsync(string.Empty)
        );

        ex.ParamName.ShouldBe("filePath");
    }

    [Fact]
    public async Task ComputeFileHashThrowsArgumentExceptionForWhitespaceFilePath()
    {
        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ComputeFileHashAsync("   ")
        );

        ex.ParamName.ShouldBe("filePath");
    }

    [Fact]
    public async Task ComputeFileHashReturnsSameHashForIdenticalFileContent()
    {
        string file1 = Path.Combine(_tempDirectory, "file1.txt");
        string file2 = Path.Combine(_tempDirectory, "file2.txt");
        const string content = "Identical content";

        File.WriteAllText(file1, content);
        File.WriteAllText(file2, content);

        string? hash1 = await _service.ComputeFileHashAsync(file1);
        string? hash2 = await _service.ComputeFileHashAsync(file2);

        hash1.ShouldBe(hash2);
    }

    [Fact]
    public async Task ComputeFileHashReturnsDifferentHashForDifferentContent()
    {
        string file1 = Path.Combine(_tempDirectory, "file1.txt");
        string file2 = Path.Combine(_tempDirectory, "file2.txt");

        File.WriteAllText(file1, "Content A");
        File.WriteAllText(file2, "Content B");

        string? hash1 = await _service.ComputeFileHashAsync(file1);
        string? hash2 = await _service.ComputeFileHashAsync(file2);

        hash1.ShouldNotBe(hash2);
    }

    [Fact]
    public async Task ComputeFileHashIsDeterministic()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(testFile, "Deterministic content");

        string? hash1 = await _service.ComputeFileHashAsync(testFile);
        string? hash2 = await _service.ComputeFileHashAsync(testFile);
        string? hash3 = await _service.ComputeFileHashAsync(testFile);

        hash1.ShouldBe(hash2);
        hash2.ShouldBe(hash3);
    }

    [Fact]
    public async Task ComputeFileHashHandlesLargeFiles()
    {
        string largeFile = Path.Combine(_tempDirectory, "large.bin");
        var largeContent = new byte[10_000_000];
        new Random(42).NextBytes(largeContent);
        File.WriteAllBytes(largeFile, largeContent);

        string? hash = await _service.ComputeFileHashAsync(largeFile);

        hash.ShouldNotBeNull();
        hash.Length.ShouldBe(64);
    }

    [Fact]
    public async Task CompareHashesReturnsTrueWhenHashesMatch()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        const string content = "Test content";
        File.WriteAllText(testFile, content);

        string? computedHash = await _service.ComputeFileHashAsync(testFile);
        bool isMatch = await _service.CompareHashesAsync(testFile, computedHash);

        isMatch.ShouldBeTrue();
    }

    [Fact]
    public async Task CompareHashesReturnsFalseWhenHashesDiffer()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(testFile, "Current content");

        bool isMatch = await _service.CompareHashesAsync(testFile, "differenthash123456789012345678");

        isMatch.ShouldBeFalse();
    }

    [Fact]
    public async Task CompareHashesReturnsFalseForNonExistentFile()
    {
        string nonExistentFile = Path.Combine(_tempDirectory, "nonexistent.txt");

        bool isMatch = await _service.CompareHashesAsync(nonExistentFile, "somehash");

        isMatch.ShouldBeFalse();
    }

    [Fact]
    public async Task CompareHashesReturnsFalseWhenRemoteHashIsNull()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(testFile, "Content");

        bool isMatch = await _service.CompareHashesAsync(testFile, null);

        isMatch.ShouldBeFalse();
    }

    [Fact]
    public async Task CompareHashesReturnsFalseWhenRemoteHashIsEmpty()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(testFile, "Content");

        bool isMatch = await _service.CompareHashesAsync(testFile, string.Empty);

        isMatch.ShouldBeFalse();
    }

    [Fact]
    public async Task CompareHashesReturnsFalseWhenRemoteHashIsWhitespace()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(testFile, "Content");

        bool isMatch = await _service.CompareHashesAsync(testFile, "   ");

        isMatch.ShouldBeFalse();
    }

    [Fact]
    public async Task CompareHashesThrowsArgumentNullExceptionForNullFilePath()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.CompareHashesAsync(null!, "hash")
        );
    }

    [Fact]
    public async Task CompareHashesThrowsArgumentExceptionForEmptyFilePath()
    {
        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CompareHashesAsync(string.Empty, "hash")
        );

        ex.ParamName.ShouldBe("filePath");
    }

    [Fact]
    public async Task CompareHashesIsCaseInsensitive()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(testFile, "Content");

        string? lowerHash = await _service.ComputeFileHashAsync(testFile);
        string? upperHash = lowerHash?.ToUpperInvariant();

        bool isMatch = await _service.CompareHashesAsync(testFile, upperHash);

        isMatch.ShouldBeTrue();
    }

    [Fact]
    public async Task HasLocalChangesReturnsTrueWhenFileIsNew()
    {
        string testFile = Path.Combine(_tempDirectory, "new.txt");
        File.WriteAllText(testFile, "New file");

        bool hasChanges = await _service.HasLocalChangesAsync(testFile, null);

        hasChanges.ShouldBeTrue();
    }

    [Fact]
    public async Task HasLocalChangesReturnsTrueWhenFileModified()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(testFile, "Original");

        string? originalHash = await _service.ComputeFileHashAsync(testFile);

        File.WriteAllText(testFile, "Modified");

        bool hasChanges = await _service.HasLocalChangesAsync(testFile, originalHash);

        hasChanges.ShouldBeTrue();
    }

    [Fact]
    public async Task HasLocalChangesReturnsFalseWhenFileUnchanged()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        const string content = "Stable content";
        File.WriteAllText(testFile, content);

        string? cachedHash = await _service.ComputeFileHashAsync(testFile);
        bool hasChanges = await _service.HasLocalChangesAsync(testFile, cachedHash);

        hasChanges.ShouldBeFalse();
    }

    [Fact]
    public async Task HasLocalChangesReturnsFalseForDeletedFile()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");

        bool hasChanges = await _service.HasLocalChangesAsync(testFile, "somehash");

        hasChanges.ShouldBeFalse();
    }

    [Fact]
    public async Task HasLocalChangesReturnsTrueWhenCachedHashIsNull()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(testFile, "Content");

        bool hasChanges = await _service.HasLocalChangesAsync(testFile, null);

        hasChanges.ShouldBeTrue();
    }

    [Fact]
    public async Task HasLocalChangesReturnsTrueWhenCachedHashIsEmpty()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(testFile, "Content");

        bool hasChanges = await _service.HasLocalChangesAsync(testFile, string.Empty);

        hasChanges.ShouldBeTrue();
    }

    [Fact]
    public async Task HasLocalChangesThrowsArgumentNullExceptionForNullFilePath()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.HasLocalChangesAsync(null!, "hash")
        );
    }

    [Fact]
    public async Task HasLocalChangesThrowsArgumentExceptionForEmptyFilePath()
    {
        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.HasLocalChangesAsync(string.Empty, "hash")
        );

        ex.ParamName.ShouldBe("filePath");
    }

    [Fact]
    public async Task HasLocalChangesIsCaseInsensitiveForHashComparison()
    {
        string testFile = Path.Combine(_tempDirectory, "test.txt");
        File.WriteAllText(testFile, "Content");

        string? lowerHash = await _service.ComputeFileHashAsync(testFile);
        string? upperHash = lowerHash?.ToUpperInvariant();

        bool hasChanges = await _service.HasLocalChangesAsync(testFile, upperHash);

        hasChanges.ShouldBeFalse();
    }

    [Fact]
    public async Task HandlesBinaryFiles()
    {
        string binaryFile = Path.Combine(_tempDirectory, "binary.bin");
        var binaryContent = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };
        File.WriteAllBytes(binaryFile, binaryContent);

        string? hash = await _service.ComputeFileHashAsync(binaryFile);

        hash.ShouldNotBeNull();
        hash.Length.ShouldBe(64);
    }

    [Fact]
    public async Task HandlesEmptyFiles()
    {
        string emptyFile = Path.Combine(_tempDirectory, "empty.txt");
        File.WriteAllText(emptyFile, string.Empty);

        string? hash = await _service.ComputeFileHashAsync(emptyFile);

        hash.ShouldNotBeNull();
        hash.Length.ShouldBe(64);
    }

    [Fact]
    public async Task CancellationTokenIsRespected()
    {
        string largeFile = Path.Combine(_tempDirectory, "large.bin");
        var largeContent = new byte[100_000_000];
        new Random(42).NextBytes(largeContent);
        File.WriteAllBytes(largeFile, largeContent);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(10));

        await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await _service.ComputeFileHashAsync(largeFile, cts.Token)
        );
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }
        catch
        {
        }
    }
}
