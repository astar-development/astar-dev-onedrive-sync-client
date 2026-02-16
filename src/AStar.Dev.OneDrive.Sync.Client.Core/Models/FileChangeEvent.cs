using AStar.Dev.OneDrive.Sync.Client.Core.Models.Enums;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Models;

/// <summary>
///     Represents a local file system change event detected by the file watcher.
/// </summary>
/// <param name="HashedAccountId">Hashed account identifier.</param>
/// <param name="LocalPath">Full local file system path.</param>
/// <param name="RelativePath">Path relative to the sync root directory.</param>
/// <param name="ChangeType">Type of file system change.</param>
/// <param name="DetectedUtc">Timestamp when the change was detected.</param>
public sealed record FileChangeEvent(
    HashedAccountId HashedAccountId,
    string LocalPath,
    string RelativePath,
    FileChangeType ChangeType,
    DateTimeOffset DetectedUtc
);
