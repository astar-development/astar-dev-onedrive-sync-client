using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

public class ConflictResolverTests
{
    [Fact]
    public void Resolve_WithIgnorePolicy_ShouldReturnSkip()
    {
        DateTimeOffset localModified = DateTimeOffset.UtcNow;
        DateTimeOffset remoteModified = DateTimeOffset.UtcNow.AddMinutes(-10);

        ConflictOutcome outcome = ConflictResolver.Resolve(ConflictPolicy.Ignore, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.Skip);
    }

    [Fact]
    public void Resolve_WithLocalWinsPolicy_ShouldReturnUseLocal()
    {
        DateTimeOffset localModified = DateTimeOffset.UtcNow;
        DateTimeOffset remoteModified = DateTimeOffset.UtcNow.AddHours(-1);

        ConflictOutcome outcome = ConflictResolver.Resolve(ConflictPolicy.LocalWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void Resolve_WithRemoteWinsPolicy_ShouldReturnUseRemote()
    {
        DateTimeOffset localModified = DateTimeOffset.UtcNow;
        DateTimeOffset remoteModified = DateTimeOffset.UtcNow.AddHours(-1);

        ConflictOutcome outcome = ConflictResolver.Resolve(ConflictPolicy.RemoteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseRemote);
    }

    [Fact]
    public void Resolve_WithKeepBothPolicy_ShouldReturnKeepBoth()
    {
        DateTimeOffset localModified = DateTimeOffset.UtcNow;
        DateTimeOffset remoteModified = DateTimeOffset.UtcNow.AddMinutes(-5);

        ConflictOutcome outcome = ConflictResolver.Resolve(ConflictPolicy.KeepBoth, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.KeepBoth);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_LocalIsNewer_ShouldReturnUseLocal()
    {
        DateTimeOffset remoteModified = DateTimeOffset.UtcNow.AddMinutes(-10);
        DateTimeOffset localModified = DateTimeOffset.UtcNow; // Newer

        ConflictOutcome outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_RemoteIsNewer_ShouldReturnUseRemote()
    {
        DateTimeOffset localModified = DateTimeOffset.UtcNow.AddMinutes(-10);
        DateTimeOffset remoteModified = DateTimeOffset.UtcNow; // Newer

        ConflictOutcome outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseRemote);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_SameTimes_ShouldReturnUseLocal()
    {
        DateTimeOffset timestamp = DateTimeOffset.UtcNow;
        DateTimeOffset localModified = timestamp;
        DateTimeOffset remoteModified = timestamp;

        ConflictOutcome outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);
        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_LocalJustOneSecondNewer_ShouldReturnUseLocal()
    {
        DateTimeOffset remoteModified = DateTimeOffset.UtcNow;
        DateTimeOffset localModified = remoteModified.AddSeconds(1);

        ConflictOutcome outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_RemoteJustOneSecondNewer_ShouldReturnUseRemote()
    {
        DateTimeOffset localModified = DateTimeOffset.UtcNow;
        DateTimeOffset remoteModified = localModified.AddSeconds(1);

        ConflictOutcome outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseRemote);
    }

    [Fact]
    public void Resolve_WithLastWriteWins_OldTimestampsVsNewTimestamps_ShouldCompareCorrectly()
    {
        var remoteModified = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var localModified = new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero);

        ConflictOutcome outcome = ConflictResolver.Resolve(ConflictPolicy.LastWriteWins, localModified, remoteModified);

        outcome.ShouldBe(ConflictOutcome.UseLocal);
    }

    [Theory]
    [InlineData(ConflictPolicy.Ignore)]
    [InlineData(ConflictPolicy.LocalWins)]
    [InlineData(ConflictPolicy.RemoteWins)]
    [InlineData(ConflictPolicy.KeepBoth)]
    [InlineData(ConflictPolicy.LastWriteWins)]
    public void Resolve_ShouldHandleAllPolicies(ConflictPolicy policy)
    {
        DateTimeOffset localModified = DateTimeOffset.UtcNow;
        DateTimeOffset remoteModified = DateTimeOffset.UtcNow.AddMinutes(-5);

        ConflictOutcome outcome = ConflictResolver.Resolve(policy, localModified, remoteModified);

        outcome.ShouldNotBe((ConflictOutcome)999);
        outcome.ShouldBeOneOf(
            ConflictOutcome.Skip,
            ConflictOutcome.UseLocal,
            ConflictOutcome.UseRemote,
            ConflictOutcome.KeepBoth);
    }

    [Fact]
    public void MakeKeepBothName_ShouldGenerateValidFilename()
    {
        var localPath = "/home/jason/Documents/report.docx";
        var localModified = new DateTimeOffset(2024, 1, 15, 14, 32, 0, TimeSpan.Zero);

        var result = ConflictResolver.MakeKeepBothName(localPath, localModified);

        result.ShouldContain("report");
        result.ShouldContain("docx");
        result.ShouldContain("local");
        result.ShouldContain("2024-01-15");
        result.ShouldContain("14-32");
    }

    [Fact]
    public void MakeKeepBothName_WithPathContainingSpaces_ShouldPreserveExtension()
    {
        var localPath = "/home/jason/My Documents/My Report.docx";
        DateTimeOffset localModified = DateTimeOffset.UtcNow;

        var result = ConflictResolver.MakeKeepBothName(localPath, localModified);

        result.ShouldContain("My Report");
        result.ShouldEndWith(".docx");
    }

    [Fact]
    public void MakeKeepBothName_WithFileHavingNoExtension_ShouldStillGenerateName()
    {
        var localPath = "/home/jason/Documents/README";
        DateTimeOffset localModified = DateTimeOffset.UtcNow;

        var result = ConflictResolver.MakeKeepBothName(localPath, localModified);

        result.ShouldContain("README");
        result.ShouldContain("local");
    }

    [Fact]
    public void MakeKeepBothName_WithMultipleDots_ShouldOnlyRemoveLastExtension()
    {
        var localPath = "/home/jason/file.tar.gz";
        DateTimeOffset localModified = DateTimeOffset.UtcNow;

        var result = ConflictResolver.MakeKeepBothName(localPath, localModified);

        result.ShouldContain("file.tar");
        result.ShouldEndWith(".gz");
    }

    [Fact]
    public void MakeKeepBothName_GeneratesIncreasinglyUniquePaths()
    {
        var localPath = "/home/jason/Documents/report.docx";
        var time1 = new DateTimeOffset(2024, 1, 15, 14, 32, 0, TimeSpan.Zero);
        var time2 = new DateTimeOffset(2024, 1, 16, 14, 32, 0, TimeSpan.Zero);

        var result1 = ConflictResolver.MakeKeepBothName(localPath, time1);
        var result2 = ConflictResolver.MakeKeepBothName(localPath, time2);

        result1.ShouldNotBe(result2);
        result1.ShouldContain("2024-01-15");
        result2.ShouldContain("2024-01-16");
    }

    [Fact]
    public void MakeKeepBothName_OutputPathIsOnSameDirectory()
    {
        var localPath = "/home/jason/Documents/report.docx";
        DateTimeOffset localModified = DateTimeOffset.UtcNow;

        var result = ConflictResolver.MakeKeepBothName(localPath, localModified);

        var resultDir = Path.GetDirectoryName(result);
        var originalDir = Path.GetDirectoryName(localPath);
        resultDir.ShouldBe(originalDir);
    }
}
