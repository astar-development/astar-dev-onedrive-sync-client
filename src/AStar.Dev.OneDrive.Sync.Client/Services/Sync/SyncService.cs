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

    // ── ISyncService ──────────────────────────────────────────────────────

    public async Task SyncAccountAsync(
        OneDriveAccount account,
        CancellationToken ct = default)
    {
        // Acquire token silently — no UI interaction
        var authResult = await authService.AcquireTokenSilentAsync(account.Id, ct);
        if (authResult.IsError)
        {
            // Token refresh failed — surface via progress event so UI can prompt
            RaiseProgress(account.Id, string.Empty, 0, 0,
                authResult.ErrorMessage ?? "Auth failed", isComplete: true);
            return;
        }

        var token = authResult.AccessToken!;

        // Validate local sync path
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

    public async Task ResolveConflictAsync(
        SyncConflict   conflict,
        ConflictPolicy policy,
        CancellationToken ct = default)
    {
        var authResult = await authService
            .AcquireTokenSilentAsync(conflict.AccountId, ct);

        if (authResult.IsError) return;

        var token   = authResult.AccessToken!;
        var outcome = ConflictResolver.Resolve(
            policy,
            conflict.LocalModified,
            conflict.RemoteModified);

        await ApplyConflictOutcomeAsync(conflict, outcome, token, ct);
        await syncRepository.ResolveConflictAsync(conflict.Id, policy);
    }

    // ── Private — folder sync ─────────────────────────────────────────────

    private async Task SyncFolderAsync(
        OneDriveAccount account,
        string          token,
        string          folderId,
        CancellationToken ct)
    {
        // Get stored delta link for this folder (null = full sync)
        var entity = await accountRepository.GetByIdAsync(account.Id);
        var folderEntity = entity?.SyncFolders
            .FirstOrDefault(f => f.FolderId == folderId);

        var deltaLink = folderEntity?.DeltaLink;

        RaiseProgress(account.Id, folderId, 0, 0, "Fetching changes\u2026");

        var delta = await graphService.GetDeltaAsync(token, folderId, deltaLink, ct);

        if (delta.Items.Count == 0)
        {
            // Persist updated delta link even if no changes
            if (delta.NextDeltaLink is not null)
                await accountRepository.UpdateDeltaLinkAsync(
                    account.Id, folderId, delta.NextDeltaLink);

            RaiseProgress(account.Id, folderId, 0, 0,
                "No changes", isComplete: true);
            return;
        }

        // Build job queue from delta items
        var jobs = BuildJobs(account, folderId, delta.Items);

        // Separate conflicts from clean jobs
        var (cleanJobs, conflicts) = await ClassifyJobsAsync(
            account, jobs, ct);

        // Queue clean jobs
        if (cleanJobs.Count > 0)
            await syncRepository.EnqueueJobsAsync(cleanJobs);

        // Queue conflicts (do NOT block — user resolves async)
        foreach (var conflict in conflicts)
            await syncRepository.AddConflictAsync(conflict);

        // Process the clean job queue
        await ProcessJobQueueAsync(account, token, cleanJobs, ct);

        // Persist updated delta link
        if (delta.NextDeltaLink is not null)
            await accountRepository.UpdateDeltaLinkAsync(
                account.Id, folderId, delta.NextDeltaLink);

        // Update last synced timestamp
        if (entity is not null)
        {
            entity.LastSyncedAt = DateTimeOffset.UtcNow;
            await accountRepository.UpsertAsync(entity);
        }

        account.LastSyncedAt = DateTimeOffset.UtcNow;
    }

    // ── Private — job building ────────────────────────────────────────────

    private static List<SyncJob> BuildJobs(
        OneDriveAccount  account,
        string           folderId,
        List<DeltaItem>  items)
    {
        List<SyncJob> jobs = [];

        foreach (var item in items)
        {
            if (item.IsFolder) continue; // folder structure handled by local mkdir

            var relativePath = item.Name; // simplified — full path resolution in step 7
            var localPath    = Path.Combine(
                account.LocalSyncPath,
                relativePath);

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

    // ── Private — conflict detection ──────────────────────────────────────

    private async Task<(List<SyncJob> Clean, List<SyncConflict> Conflicts)>
        ClassifyJobsAsync(
            OneDriveAccount account,
            List<SyncJob>   jobs,
            CancellationToken ct)
    {
        List<SyncJob>      clean     = [];
        List<SyncConflict> conflicts = [];

        foreach (var job in jobs)
        {
            if (job.Direction == SyncDirection.Delete ||
                !File.Exists(job.LocalPath))
            {
                clean.Add(job);
                continue;
            }

            var localInfo     = new FileInfo(job.LocalPath);
            var localModified = new DateTimeOffset(
                localInfo.LastWriteTimeUtc, TimeSpan.Zero);

            // Conflict: local file modified more recently than the
            // last known remote state (approximated by remote modified time)
            var isConflict = localModified > job.RemoteModified.AddSeconds(-5);

            if (!isConflict)
            {
                clean.Add(job);
                continue;
            }

            // Apply account conflict policy immediately
            var outcome = ConflictResolver.Resolve(
                account.ConflictPolicy,
                localModified,
                job.RemoteModified);

            switch (outcome)
            {
                case ConflictOutcome.Skip:
                    // Queue as skipped — record it but don't download
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
                    clean.Add(job with
                    {
                        Direction = SyncDirection.Upload
                    });
                    break;

                case ConflictOutcome.KeepBoth:
                    // Rename local then download remote
                    var newName = ConflictResolver.MakeKeepBothName(
                        job.LocalPath, localModified);
                    File.Move(job.LocalPath, newName);
                    clean.Add(job);
                    break;
            }
        }

        await Task.CompletedTask; // async signature for future expansion
        return (clean, conflicts);
    }

    // ── Private — job processing ──────────────────────────────────────────

    private async Task ProcessJobQueueAsync(
        OneDriveAccount account,
        string          token,
        List<SyncJob>   jobs,
        CancellationToken ct)
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
                await syncRepository.UpdateJobStateAsync(
                    job.Id, SyncJobState.Completed);
            }
            catch (Exception ex)
            {
                await syncRepository.UpdateJobStateAsync(
                    job.Id, SyncJobState.Failed, ex.Message);
            }

            completed++;
        }

        RaiseProgress(account.Id,
            jobs.FirstOrDefault()?.FolderId ?? string.Empty,
            completed, total, string.Empty, isComplete: true);

        await syncRepository.ClearCompletedJobsAsync(account.Id);
    }

    private static async Task ExecuteJobAsync(
        SyncJob job,
        string  token,
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
                // Upload implementation wired in a later step
                // when we add write scopes to the Graph service
                break;
        }
    }

    private static async Task DownloadFileAsync(SyncJob job, CancellationToken ct)
    {
        if (job.DownloadUrl is null) return;

        var dir = Path.GetDirectoryName(job.LocalPath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        using var http     = new HttpClient();
        using var response = await http.GetAsync(
            job.DownloadUrl,
            HttpCompletionOption.ResponseHeadersRead, ct);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        await using var file   = File.Create(job.LocalPath);
        await stream.CopyToAsync(file, ct);

        // Preserve remote last-modified timestamp
        File.SetLastWriteTimeUtc(
            job.LocalPath,
            job.RemoteModified.UtcDateTime);
    }

    private async Task ApplyConflictOutcomeAsync(
        SyncConflict      conflict,
        ConflictOutcome   outcome,
        string            token,
        CancellationToken ct)
    {
        switch (outcome)
        {
            case ConflictOutcome.UseRemote:
                var downloadJob = new SyncJob
                {
                    AccountId      = conflict.AccountId,
                    FolderId       = conflict.FolderId,
                    RemoteItemId   = conflict.RemoteItemId,
                    RelativePath   = conflict.RelativePath,
                    LocalPath      = conflict.LocalPath,
                    Direction      = SyncDirection.Download,
                    RemoteModified = conflict.RemoteModified
                };
                await ExecuteJobAsync(downloadJob, token, ct);
                break;

            case ConflictOutcome.KeepBoth:
                var keepBothName = ConflictResolver.MakeKeepBothName(
                    conflict.LocalPath, conflict.LocalModified);
                if (File.Exists(conflict.LocalPath))
                    File.Move(conflict.LocalPath, keepBothName);
                break;

            case ConflictOutcome.Skip:
            case ConflictOutcome.UseLocal:
            default:
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
