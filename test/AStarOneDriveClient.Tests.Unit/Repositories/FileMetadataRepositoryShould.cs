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
        using var context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc1.txt"));
        await repository.AddAsync(CreateFileMetadata("file2", "acc1", "/doc2.txt"));
        await repository.AddAsync(CreateFileMetadata("file3", "acc2", "/doc3.txt"));

        var result = await repository.GetByAccountIdAsync("acc1");

        result.Count.ShouldBe(2);
        result.ShouldContain(f => f.Id == "file1");
        result.ShouldContain(f => f.Id == "file2");
    }

    [Fact]
    public async Task GetFileByIdCorrectly()
    {
        using var context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc.txt"));

        var result = await repository.GetByIdAsync("file1");

        result.ShouldNotBeNull();
        result.Id.ShouldBe("file1");
        result.Path.ShouldBe("/doc.txt");
    }

    [Fact]
    public async Task ReturnNullWhenFileNotFoundById()
    {
        using var context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);

        var result = await repository.GetByIdAsync("nonexistent");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetFileByPathCorrectly()
    {
        using var context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/docs/file.txt"));

        var result = await repository.GetByPathAsync("acc1", "/docs/file.txt");

        result.ShouldNotBeNull();
        result.Id.ShouldBe("file1");
    }

    [Fact]
    public async Task ReturnNullWhenFileNotFoundByPath()
    {
        using var context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);

        var result = await repository.GetByPathAsync("acc1", "/nonexistent.txt");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetFilesByStatusCorrectly()
    {
        using var context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc1.txt", FileSyncStatus.Synced));
        await repository.AddAsync(CreateFileMetadata("file2", "acc1", "/doc2.txt", FileSyncStatus.PendingUpload));
        await repository.AddAsync(CreateFileMetadata("file3", "acc1", "/doc3.txt", FileSyncStatus.Conflict));

        var result = await repository.GetByStatusAsync("acc1", FileSyncStatus.PendingUpload);

        result.Count.ShouldBe(1);
        result[0].Id.ShouldBe("file2");
    }

    [Fact]
    public async Task AddFileMetadataSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        var file = CreateFileMetadata("file1", "acc1", "/doc.txt");

        await repository.AddAsync(file);

        var result = await repository.GetByIdAsync("file1");
        result.ShouldNotBeNull();
        result.Name.ShouldBe("doc.txt");
    }

    [Fact]
    public async Task UpdateFileMetadataSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc.txt", FileSyncStatus.PendingUpload));

        var updated = new FileMetadata("file1", "acc1", "doc.txt", "/doc.txt", 2048, DateTime.UtcNow, @"C:\local\doc.txt", "newtag", "newetag", "newhash", FileSyncStatus.Synced, SyncDirection.Upload);
        await repository.UpdateAsync(updated);

        var result = await repository.GetByIdAsync("file1");
        result.ShouldNotBeNull();
        result.SyncStatus.ShouldBe(FileSyncStatus.Synced);
        result.Size.ShouldBe(2048);
        result.LastSyncDirection.ShouldBe(SyncDirection.Upload);
    }

    [Fact]
    public async Task ThrowExceptionWhenUpdatingNonExistentFile()
    {
        using var context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        var file = CreateFileMetadata("nonexistent", "acc1", "/doc.txt");

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await repository.UpdateAsync(file)
        );

        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task DeleteFileMetadataSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc.txt"));

        await repository.DeleteAsync("file1");

        var result = await repository.GetByIdAsync("file1");
        result.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAllFilesForAccountSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/doc1.txt"));
        await repository.AddAsync(CreateFileMetadata("file2", "acc1", "/doc2.txt"));
        await repository.AddAsync(CreateFileMetadata("file3", "acc2", "/doc3.txt"));

        await repository.DeleteByAccountIdAsync("acc1");

        var acc1Files = await repository.GetByAccountIdAsync("acc1");
        var acc2Files = await repository.GetByAccountIdAsync("acc2");
        acc1Files.ShouldBeEmpty();
        acc2Files.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveBatchUpsertFilesCorrectly()
    {
        using var context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);
        await repository.AddAsync(CreateFileMetadata("file1", "acc1", "/old.txt", FileSyncStatus.Synced));

        var batchFiles = new[]
        {
            CreateFileMetadata("file1", "acc1", "/old.txt", FileSyncStatus.PendingUpload),
            CreateFileMetadata("file2", "acc1", "/new.txt", FileSyncStatus.PendingDownload)
        };
        await repository.SaveBatchAsync(batchFiles);

        var allFiles = await repository.GetByAccountIdAsync("acc1");
        allFiles.Count.ShouldBe(2);

        var file1 = await repository.GetByIdAsync("file1");
        file1.ShouldNotBeNull();
        file1.SyncStatus.ShouldBe(FileSyncStatus.PendingUpload);
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionForNullParameters()
    {
        using var context = CreateInMemoryContext();
        var repository = new FileMetadataRepository(context);

        await Should.ThrowAsync<ArgumentNullException>(async () => await repository.AddAsync(null!));
        await Should.ThrowAsync<ArgumentNullException>(async () => await repository.GetByAccountIdAsync(null!));
        await Should.ThrowAsync<ArgumentNullException>(async () => await repository.GetByIdAsync(null!));
    }

    private static FileMetadata CreateFileMetadata(string id, string accountId, string path, FileSyncStatus status = FileSyncStatus.Synced) =>
        new(id, accountId, System.IO.Path.GetFileName(path), path, 1024, DateTime.UtcNow, $@"C:\local{path}", "ctag", "etag", "hash", status, null);

    private static SyncDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SyncDbContext(options);
    }
}
