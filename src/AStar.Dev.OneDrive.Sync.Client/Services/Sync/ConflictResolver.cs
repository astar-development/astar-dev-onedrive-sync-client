using AStar.Dev.OneDrive.Sync.Client.Models;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Sync;

public static class ConflictResolver
{
    public static ConflictOutcome Resolve(
        ConflictPolicy    policy,
        DateTimeOffset    localModified,
        DateTimeOffset    remoteModified) =>
        policy switch
        {
            ConflictPolicy.Ignore        => ConflictOutcome.Skip,
            ConflictPolicy.LocalWins     => ConflictOutcome.UseLocal,
            ConflictPolicy.RemoteWins    => ConflictOutcome.UseRemote,
            ConflictPolicy.KeepBoth      => ConflictOutcome.KeepBoth,
            ConflictPolicy.LastWriteWins => localModified >= remoteModified
                                               ? ConflictOutcome.UseLocal
                                               : ConflictOutcome.UseRemote,
            _                            => ConflictOutcome.Skip
        };

    /// <summary>
    /// Generates a "keep both" filename by appending a timestamp suffix.
    /// e.g. report.docx → report (local 2024-01-15 14-32).docx
    /// </summary>
    public static string MakeKeepBothName(string localPath, DateTimeOffset localModified)
    {
        var dir       = Path.GetDirectoryName(localPath) ?? string.Empty;
        var stem      = Path.GetFileNameWithoutExtension(localPath);
        var ext       = Path.GetExtension(localPath);
        var timestamp = localModified.LocalDateTime.ToString("yyyy-MM-dd HH-mm");
        return Path.Combine(dir, $"{stem} (local {timestamp}){ext}");
    }
}
