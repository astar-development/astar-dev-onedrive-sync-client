using System.Reflection;
using AStar.Dev.OneDrive.Sync.Client.Services.Sync;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Services.Sync;

file static class UploadServiceTestExtensions
{
    internal static Task<string> UploadChunksDirectAsync(
        this UploadService sut,
        string sessionUrl,
        string localPath,
        CancellationToken ct,
        IProgress<long>? progress = null)
    {
        var fileInfo = new FileInfo(localPath);
        var totalBytes = fileInfo.Length;

        var method = typeof(UploadService).GetMethod(
            "UploadChunksAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        return (Task<string>)method.Invoke(sut, [sessionUrl, localPath, totalBytes, progress, ct])!;
    }
}
