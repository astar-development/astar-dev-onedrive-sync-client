namespace AStarOneDriveClient.Data.Entities;

/// <summary>
/// Entity for storing current sync state and progress in the database.
/// </summary>
public sealed class SyncStateEntity
{
    public required string AccountId { get; set; }
    public int Status { get; set; }
    public int TotalFiles { get; set; }
    public int CompletedFiles { get; set; }
    public long TotalBytes { get; set; }
    public long CompletedBytes { get; set; }
    public int FilesDownloading { get; set; }
    public int FilesUploading { get; set; }
    public int ConflictsDetected { get; set; }
    public double MegabytesPerSecond { get; set; }
    public int? EstimatedSecondsRemaining { get; set; }
    public DateTime? LastUpdateUtc { get; set; }
}
