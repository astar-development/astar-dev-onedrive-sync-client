using AStar.Dev.OneDrive.Sync.Client.Data.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Models;
using AStar.Dev.OneDrive.Sync.Client.Services.Auth;
using AStar.Dev.OneDrive.Sync.Client.Services.Graph;

namespace AStar.Dev.OneDrive.Sync.Client.Services.Sync;

public sealed class SyncService(
    IAuthService       authService,
    IGraphService      graphService,
    IAccountRepository accountRepository,
    ISyncRepository    syncRepository) : ISyncService
{
    public event EventHandler<SyncProgressEventArgs>? SyncProgressChanged;
    public event EventHandler<JobCompletedEventArgs>? JobCompleted;
    public event EventHandler<SyncConflict>?          ConflictDetected;

    // ── ISyncService ──────────────────────────────────────────────────────

    public async Task SyncAccountAsync(OneDriveAccount account, CancellationToken ct = default)
    {
        var authResult = await authService.AcquireTokenSilentAsync(account.Id, ct);
        if (authResult.IsError)
        {
            RaiseProgress(account.Id, string.Empty, 0, 0,
                authResult.ErrorMessage ?? "Auth failed", isComplete: true);

            return;
        }

        var token = authResult.AccessToken!;

        if (string.IsNullOrEmpty(account.LocalSyncPath))
        {
            RaiseProgress(account.Id, string.Empty, 0, 0,
                "No local sync path configured", isComplete: true);

            return;
        }

        foreach (var folderId in account.SelectedFolderIds)
        {
            if (ct.IsCancellationRequested) break;

            await SyncFolderAsync(account, token, folderId, ct);
        }
    }

    public async Task ResolveConflictAsync(SyncConflict conflict, ConflictPolicy policy, CancellationToken ct = default)
    {
        var authResult = await authService.AcquireTokenSilentAsync(conflict.AccountId, ct);

        if (authResult.IsError) return;

        var outcome = ConflictResolver.Resolve(policy, conflict.LocalModified, conflict.RemoteModified);

        await ApplyConflictOutcomeAsync(conflict, outcome, authResult.AccessToken!, ct);

        await syncRepository.ResolveConflictAsync(conflict.Id, policy);
    }

    // ── Folder sync ───────────────────────────────────────────────────────

    private async Task SyncFolderAsync(OneDriveAccount account, string token, string folderId, CancellationToken ct)
    {
        var entity       = await accountRepository.GetByIdAsync(account.Id);
        var folderEntity = entity?.SyncFolders.FirstOrDefault(f => f.FolderId == folderId);

        var deltaLink = folderEntity?.DeltaLink;

        RaiseProgress(account.Id, folderId, 0, 0, "Fetching changes\u2026");

        var delta = await graphService.GetDeltaAsync(
            token, folderId, deltaLink, ct);

        if (delta.Items.Count == 0)
        {
            if (delta.NextDeltaLink is not null)
                await accountRepository.UpdateDeltaLinkAsync(
                    account.Id, folderId, delta.NextDeltaLink);

            RaiseProgress(account.Id, folderId, 0, 0,
                "No changes", isComplete: true);

            return;
        }

        var jobs = BuildJobs(account, folderId, delta.Items);
        var (cleanJobs, conflicts) = await ClassifyJobsAsync(account, jobs, ct);

        if (cleanJobs.Count > 0)
            await syncRepository.EnqueueJobsAsync(cleanJobs);

        foreach (var conflict in conflicts)
        {
            await syncRepository.AddConflictAsync(conflict);
            ConflictDetected?.Invoke(this, conflict);
        }

        await ProcessJobQueueAsync(account, token, cleanJobs, ct);

        if (delta.NextDeltaLink is not null)
            await accountRepository.UpdateDeltaLinkAsync(
                account.Id, folderId, delta.NextDeltaLink);

        if (entity is not null)
        {
            entity.LastSyncedAt = DateTimeOffset.UtcNow;
            await accountRepository.UpsertAsync(entity);
        }

        account.LastSyncedAt = DateTimeOffset.UtcNow;
    }

    // ── Job building ──────────────────────────────────────────────────────

    private static List<SyncJob> BuildJobs(OneDriveAccount account, string folderId, List<DeltaItem> items)
    {
        List<SyncJob> jobs = [];

        foreach (var item in items)
        {
            if (item.IsFolder) continue;

            var relativePath = item.Name;
            var localPath    = Path.Combine(account.LocalSyncPath, relativePath);

            if (item.IsDeleted)
            {
                if (File.Exists(localPath))
                    jobs.Add(new SyncJob
                    {
                        AccountId      = account.Id,
                        FolderId       = folderId,
                        RemoteItemId   = item.Id,
                        RelativePath   = relativePath,
                        LocalPath      = localPath,
                        Direction      = SyncDirection.Delete,
                        RemoteModified = item.LastModified ?? DateTimeOffset.UtcNow
                    });
            }
            else
            {
                jobs.Add(new SyncJob
                {
                    AccountId      = account.Id,
                    FolderId       = folderId,
                    RemoteItemId   = item.Id,
                    RelativePath   = relativePath,
                    LocalPath      = localPath,
                    Direction      = SyncDirection.Download,
                    DownloadUrl    = item.DownloadUrl,
                    FileSize       = item.Size,
                    RemoteModified = item.LastModified ?? DateTimeOffset.UtcNow
                });
            }
        }

        return jobs;
    }

    // ── Conflict detection ────────────────────────────────────────────────

    private async Task<(List<SyncJob> Clean, List<SyncConflict> Conflicts)>
        ClassifyJobsAsync(OneDriveAccount account, List<SyncJob> jobs, CancellationToken ct)
    {
        List<SyncJob>      clean     = [];
        List<SyncConflict> conflicts = [];

        foreach (var job in jobs)
        {
            if (job.Direction == SyncDirection.Delete || !File.Exists(job.LocalPath))
            {
                clean.Add(job);
                continue;
            }

            var localInfo     = new FileInfo(job.LocalPath);
            var localModified = new DateTimeOffset(
                localInfo.LastWriteTimeUtc, TimeSpan.Zero);

            var isConflict = localModified > job.RemoteModified.AddSeconds(-5);

            if (!isConflict)
            {
                clean.Add(job);
                continue;
            }

            var outcome = ConflictResolver.Resolve(
                account.ConflictPolicy,
                localModified,
                job.RemoteModified);

            switch (outcome)
            {
                case ConflictOutcome.Skip:
                    conflicts.Add(new SyncConflict
                    {
                        AccountId      = account.Id,
                        FolderId       = job.FolderId,
                        RemoteItemId   = job.RemoteItemId,
                        RelativePath   = job.RelativePath,
                        LocalPath      = job.LocalPath,
                        LocalModified  = localModified,
                        RemoteModified = job.RemoteModified,
                        LocalSize      = localInfo.Length,
                        RemoteSize     = job.FileSize
                    });
                    break;

                case ConflictOutcome.UseRemote:
                    clean.Add(job);
                    break;

                case ConflictOutcome.UseLocal:
                    clean.Add(job with { Direction = SyncDirection.Upload });
                    break;

                case ConflictOutcome.KeepBoth:
                    var newName = ConflictResolver.MakeKeepBothName(
                        job.LocalPath, localModified);
                    File.Move(job.LocalPath, newName);
                    clean.Add(job);
                    break;
            }
        }

        await Task.CompletedTask;

        return (clean, conflicts);
    }

    // ── Job processing ────────────────────────────────────────────────────

    private async Task ProcessJobQueueAsync(OneDriveAccount account, string token, List<SyncJob> jobs, CancellationToken ct)
    {
        var completed = 0;
        var total     = jobs.Count;

        foreach (var job in jobs)
        {
            if (ct.IsCancellationRequested) break;

            RaiseProgress(account.Id, job.FolderId,
                completed, total, job.RelativePath);

            await syncRepository.UpdateJobStateAsync(
                job.Id, SyncJobState.InProgress);

            try
            {
                await ExecuteJobAsync(job, token, ct);

                var completedJob = job with
                {
                    State       = SyncJobState.Completed,
                    CompletedAt = DateTimeOffset.UtcNow
                };

                await syncRepository.UpdateJobStateAsync(
                    job.Id, SyncJobState.Completed);

                JobCompleted?.Invoke(this,
                    new JobCompletedEventArgs(completedJob));
            }
            catch (Exception ex)
            {
                var failedJob = job with
                {
                    State        = SyncJobState.Failed,
                    ErrorMessage = ex.Message,
                    CompletedAt  = DateTimeOffset.UtcNow
                };

                await syncRepository.UpdateJobStateAsync(
                    job.Id, SyncJobState.Failed, ex.Message);

                JobCompleted?.Invoke(this,
                    new JobCompletedEventArgs(failedJob));
            }

            completed++;
        }

        RaiseProgress(account.Id,
            jobs.FirstOrDefault()?.FolderId ?? string.Empty,
            completed, total, string.Empty, isComplete: true);

        await syncRepository.ClearCompletedJobsAsync(account.Id);
    }

    private static async Task ExecuteJobAsync(
        SyncJob           job,
        string            token,
        CancellationToken ct)
    {
        switch (job.Direction)
        {
            case SyncDirection.Download:
                await DownloadFileAsync(job, ct);
                break;
            case SyncDirection.Delete:
                if (File.Exists(job.LocalPath))
                    File.Delete(job.LocalPath);
                break;
            case SyncDirection.Upload:
                // Upload wired in a later step
                break;
        }
    }

    private static async Task DownloadFileAsync(SyncJob job, CancellationToken ct)
    {
        if (job.DownloadUrl is null) return;

        var dir = Path.GetDirectoryName(job.LocalPath);
        if (dir is not null) Directory.CreateDirectory(dir);

        using var http     = new HttpClient();
        using var response = await http.GetAsync(
            job.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead, ct);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var file   = File.Create(job.LocalPath);
        await stream.CopyToAsync(file, ct);

        File.SetLastWriteTimeUtc(job.LocalPath, job.RemoteModified.UtcDateTime);
    }

    private async Task ApplyConflictOutcomeAsync(SyncConflict conflict, ConflictOutcome outcome, string token, CancellationToken ct)
    {
        switch (outcome)
        {
            case ConflictOutcome.UseRemote:
                var job = new SyncJob
                {
                    AccountId      = conflict.AccountId,
                    FolderId       = conflict.FolderId,
                    RemoteItemId   = conflict.RemoteItemId,
                    RelativePath   = conflict.RelativePath,
                    LocalPath      = conflict.LocalPath,
                    Direction      = SyncDirection.Download,
                    RemoteModified = conflict.RemoteModified
                };
                await ExecuteJobAsync(job, token, ct);
                break;

            case ConflictOutcome.KeepBoth:
                var keepBothName = ConflictResolver.MakeKeepBothName(
                    conflict.LocalPath, conflict.LocalModified);
                if (File.Exists(conflict.LocalPath))
                    File.Move(conflict.LocalPath, keepBothName);
                break;
        }
    }

    private void RaiseProgress(
        string accountId,
        string folderId,
        int    completed,
        int    total,
        string currentFile,
        bool   isComplete = false) =>
        SyncProgressChanged?.Invoke(this, new SyncProgressEventArgs(
            accountId, folderId, completed, total, currentFile, isComplete));
}
