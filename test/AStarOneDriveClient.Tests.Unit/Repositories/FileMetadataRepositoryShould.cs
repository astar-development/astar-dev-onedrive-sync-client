using AStarOneDriveClient.Data;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Models.Enums;
using AStarOneDriveClient.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AStarOneDriveClient.Tests.Unit.Repositories;

public class FileMetadataRepositoryShould
{
    [Fact]
    public async Task GetFilesByAccountIdCorrectly()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc1.txt"), TestContext.Current.CancellationToken);
        await repository.AddAsync(CreateFileMetadata("file2", "acc1", "/doc2.txt"), TestContext.Current.CancellationToken);
        await repository.AddAsync(CreateFileMetadata("file3", "acc2", "/doc3.txt"), TestContext.Current.CancellationToken);

        IReadOnlyList<FileMetadata> result = await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result.ShouldContain(f => f.Id == "file1");
        result.ShouldContain(f => f.Id == "file2");
    }

    [Fact]
    public async Task GetFileByIdCorrectly()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc.txt"), TestContext.Current.CancellationToken);

        FileMetadata? result = await repository.GetByIdAsync("file1", TestContext.Current.CancellationToken);

        _ = result.ShouldNotBeNull();
        result.Id.ShouldBe("file1");
        result.Path.ShouldBe("/doc.txt");
    }

    [Fact]
    public async Task ReturnNullWhenFileNotFoundById()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);

        FileMetadata? result = await repository.GetByIdAsync("nonexistent", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetFileByPathCorrectly()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/docs/file.txt"), TestContext.Current.CancellationToken);

        FileMetadata? result = await repository.GetByPathAsync("acc1", "/docs/file.txt", TestContext.Current.CancellationToken);

        _ = result.ShouldNotBeNull();
        result.Id.ShouldBe("file1");
    }

    [Fact]
    public async Task ReturnNullWhenFileNotFoundByPath()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);

        FileMetadata? result = await repository.GetByPathAsync("acc1", "/nonexistent.txt", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetFilesByStatusCorrectly()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc1.txt"), TestContext.Current.CancellationToken);
        await repository.AddAsync(CreateFileMetadata("file2", "acc1", "/doc2.txt", FileSyncStatus.PendingUpload), TestContext.Current.CancellationToken);
        await repository.AddAsync(CreateFileMetadata("file3", "acc1", "/doc3.txt", FileSyncStatus.Conflict), TestContext.Current.CancellationToken);

        IReadOnlyList<FileMetadata> result = await repository.GetByStatusAsync("acc1", FileSyncStatus.PendingUpload, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe("file2");
    }

    [Fact]
    public async Task AddFileMetadataSuccessfully()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
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
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc.txt", FileSyncStatus.PendingUpload), TestContext.Current.CancellationToken);

        var updated = new FileMetadata("file1", "acc1", "doc.txt", "/doc.txt", 2048, DateTime.UtcNow, @"C:\local\doc.txt", "newtag", "newetag", "newhash", FileSyncStatus.Synced, SyncDirection.Upload);
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
        var repository = new FileMetadataRepository(context);
        FileMetadata file = CreateFileMetadata("nonexistent", "acc1", "/doc.txt");

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(async () => await repository.UpdateAsync(file)
        );

        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task DeleteFileMetadataSuccessfully()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc.txt"), TestContext.Current.CancellationToken);

        await repository.DeleteAsync("file1", TestContext.Current.CancellationToken);

        FileMetadata? result = await repository.GetByIdAsync("file1", TestContext.Current.CancellationToken);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAllFilesForAccountSuccessfully()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc1.txt"), TestContext.Current.CancellationToken);
        await repository.AddAsync(CreateFileMetadata("file2", "acc1", "/doc2.txt"), TestContext.Current.CancellationToken);
        await repository.AddAsync(CreateFileMetadata("file3", "acc2", "/doc3.txt"), TestContext.Current.CancellationToken);

        await repository.DeleteByAccountIdAsync("acc1", TestContext.Current.CancellationToken);

        IReadOnlyList<FileMetadata> acc1Files = await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken);
        IReadOnlyList<FileMetadata> acc2Files = await repository.GetByAccountIdAsync("acc2", TestContext.Current.CancellationToken);
        acc1Files.ShouldBeEmpty();
        acc2Files.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveBatchUpsertFilesCorrectly()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/old.txt"), TestContext.Current.CancellationToken);

        FileMetadata[] batchFiles = new[]
        {
            CreateFileMetadata("file1", "acc1", "/old.txt", FileSyncStatus.PendingUpload), CreateFileMetadata("file2", "acc1", "/new.txt", FileSyncStatus.PendingDownload)
        };
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
        var repository = new FileMetadataRepository(context);

        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await repository.AddAsync(null!));
        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await repository.GetByAccountIdAsync(null!));
        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await repository.GetByIdAsync(null!));
    }

    private static FileMetadata CreateFileMetadata(string id, string accountId, string path, FileSyncStatus status = FileSyncStatus.Synced)
        => new(id, accountId, Path.GetFileName(path), path, 1024, DateTime.UtcNow, $@"C:\local{path}", "ctag", "etag", "hash", status, null);

    private static SyncDbContext CreateInMemoryContext()
    {
        DbContextOptions<SyncDbContext> options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new SyncDbContext(options);
    }
}
