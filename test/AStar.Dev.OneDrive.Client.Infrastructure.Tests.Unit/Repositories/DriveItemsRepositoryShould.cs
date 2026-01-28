using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Core.Models.Enums;
using AStar.Dev.OneDrive.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Repositories;

public class DriveItemsRepositoryShould
{
    private readonly IDbContextFactory<SyncDbContext> _contextFactory;

    public DriveItemsRepositoryShould() => _contextFactory = new PooledDbContextFactory<SyncDbContext>(
            new DbContextOptionsBuilder<SyncDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options);

    [Fact]
    public async Task GetFilesByAccountIdCorrectly()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new DriveItemsRepository(_contextFactory);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc1.txt"), TestContext.Current.CancellationToken);
        await repository.AddAsync(CreateFileMetadata("file2", "acc1", "/doc2.txt"), TestContext.Current.CancellationToken);
        await repository.AddAsync(CreateFileMetadata("file3", "acc2", "/doc3.txt"), TestContext.Current.CancellationToken);

        IReadOnlyList<FileMetadata> result = await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result.ShouldContain(f => f.DriveItemId == "file1");
        result.ShouldContain(f => f.DriveItemId == "file2");
    }

    [Fact]
    public async Task GetFileByIdCorrectly()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new DriveItemsRepository(_contextFactory);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc.txt"), TestContext.Current.CancellationToken);

        FileMetadata? result = await repository.GetByIdAsync("file1", TestContext.Current.CancellationToken);

        _ = result.ShouldNotBeNull();
        result.DriveItemId.ShouldBe("file1");
        result.RelativePath.ShouldBe("/doc.txt");
    }

    [Fact]
    public async Task ReturnNullWhenFileNotFoundById()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new DriveItemsRepository(_contextFactory);

        FileMetadata? result = await repository.GetByIdAsync("nonexistent", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetFileByPathCorrectly()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new DriveItemsRepository(_contextFactory);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/docs/file.txt"), TestContext.Current.CancellationToken);

        FileMetadata? result = await repository.GetByPathAsync("acc1", "/docs/file.txt", TestContext.Current.CancellationToken);

        _ = result.ShouldNotBeNull();
        result.DriveItemId.ShouldBe("file1");
    }

    [Fact]
    public async Task ReturnNullWhenFileNotFoundByPath()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new DriveItemsRepository(_contextFactory);

        FileMetadata? result = await repository.GetByPathAsync("acc1", "/nonexistent.txt", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task AddFileMetadataSuccessfully()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new DriveItemsRepository(_contextFactory);
        FileMetadata file = CreateFileMetadata("file1", "acc1", "/doc.txt");

        await repository.AddAsync(file, TestContext.Current.CancellationToken);

        FileMetadata? result = await repository.GetByIdAsync("file1", TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.Name.ShouldBe("doc.txt");
    }

    [Fact]
    public async Task UpdateFileMetadataSuccessfully()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new DriveItemsRepository(_contextFactory);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc.txt", FileSyncStatus.PendingUpload), TestContext.Current.CancellationToken);

        var updated = new FileMetadata("file1", "acc1", "doc.txt", "/doc.txt", 2048, DateTime.UtcNow, @"C:\local\doc.txt", false,false,false, "newtag", "newetag", "newhash", null,FileSyncStatus.Synced, SyncDirection.Upload);
        await repository.UpdateAsync(updated, TestContext.Current.CancellationToken);

        FileMetadata? result = await repository.GetByIdAsync("file1", TestContext.Current.CancellationToken);
        _ = result.ShouldNotBeNull();
        result.SyncStatus.ShouldBe(FileSyncStatus.Synced);
        result.Size.ShouldBe(2048);
        result.LastSyncDirection.ShouldBe(SyncDirection.Upload);
    }

    [Fact]
    public async Task ThrowExceptionWhenUpdatingNonExistentFile()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new DriveItemsRepository(_contextFactory);
        FileMetadata file = CreateFileMetadata("nonexistent", "acc1", "/doc.txt");

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(async () => await repository.UpdateAsync(file)
        );

        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task DeleteFileMetadataSuccessfully()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new DriveItemsRepository(_contextFactory);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc.txt"), TestContext.Current.CancellationToken);

        await repository.DeleteAsync("file1", TestContext.Current.CancellationToken);

        FileMetadata? result = await repository.GetByIdAsync("file1", TestContext.Current.CancellationToken);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task SaveBatchUpsertFilesCorrectly()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new DriveItemsRepository(_contextFactory);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/old.txt"), TestContext.Current.CancellationToken);

        FileMetadata[] batchFiles =
        [
            CreateFileMetadata("file1", "acc1", "/old.txt", FileSyncStatus.PendingUpload), CreateFileMetadata("file2", "acc1", "/new.txt", FileSyncStatus.PendingDownload)
        ];
        await repository.SaveBatchAsync(batchFiles, TestContext.Current.CancellationToken);

        IReadOnlyList<FileMetadata> allFiles = await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken);
        allFiles.Count.ShouldBe(2);

        FileMetadata? file1 = await repository.GetByIdAsync("file1", TestContext.Current.CancellationToken);
        _ = file1.ShouldNotBeNull();
        file1.SyncStatus.ShouldBe(FileSyncStatus.PendingUpload);
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionForNullParameters()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new DriveItemsRepository(_contextFactory);

        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await repository.AddAsync(null!));
        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await repository.GetByAccountIdAsync(null!));
        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await repository.GetByIdAsync(null!));
    }

    private static FileMetadata CreateFileMetadata(string id, string accountId, string path, FileSyncStatus status = FileSyncStatus.Synced)
        => new(id, accountId, Path.GetFileName(path), path, 1024, DateTime.UtcNow, $@"C:\local{path}", false, false, false, "ctag", "etag", "hash", null, status, null);

    private static SyncDbContext CreateInMemoryContext()
    {
        DbContextOptions<SyncDbContext> options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new SyncDbContext(options);
    }
}
