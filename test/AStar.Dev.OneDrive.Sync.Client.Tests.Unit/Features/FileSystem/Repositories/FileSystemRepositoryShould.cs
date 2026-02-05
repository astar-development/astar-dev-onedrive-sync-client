using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.FileSystem.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.FileSystem.Repositories;

public class FileSystemRepositoryShould : IDisposable
{
    private readonly OneDriveSyncDbContext _context;
    private readonly IFileSystemRepository _repository;

    public FileSystemRepositoryShould()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new OneDriveSyncDbContext(options);
        _repository = new FileSystemRepository(_context);
    }

    [Fact]
    public async Task CreateFileSystemItemAndPersistToDatabase()
    {
        var item = new FileSystemItem
        {
            Id = "test-item-123",
            HashedAccountId = "hash123",
            DriveItemId = "drive-item-456",
            Name = "TestFile.txt",
            Path = "/TestFile.txt",
            IsFolder = false,
            IsSelected = true
        };

        await _repository.CreateAsync(item);

        FileSystemItem? retrieved = await _context.FileSystemItems.FindAsync(item.Id);
        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(item.Id);
        retrieved.Name.ShouldBe(item.Name);
    }

    [Fact]
    public async Task GetFileSystemItemByIdReturnsItemWhenExists()
    {
        var item = new FileSystemItem
        {
            Id = "test-id-789",
            HashedAccountId = "hash-abc",
            DriveItemId = "drive-xyz",
            Name = "Document.docx",
            Path = "/Documents/Document.docx",
            IsFolder = false
        };

        await _context.FileSystemItems.AddAsync(item);
        await _context.SaveChangesAsync();

        FileSystemItem? result = await _repository.GetByIdAsync(item.Id);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(item.Id);
        result.Name.ShouldBe(item.Name);
    }

    [Fact]
    public async Task GetFileSystemItemByIdReturnsNullWhenNotExists()
    {
        FileSystemItem? result = await _repository.GetByIdAsync("non-existent-id");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetSelectedItemsByHashedAccountIdReturnsOnlySelectedItems()
    {
        const string hashedAccountId = "test-account-hash";

        var selectedItem1 = new FileSystemItem
        {
            Id = "selected-1",
            HashedAccountId = hashedAccountId,
            DriveItemId = "drive-1",
            Name = "Selected1.txt",
            Path = "/Selected1.txt",
            IsSelected = true,
            IsFolder = false
        };

        var selectedItem2 = new FileSystemItem
        {
            Id = "selected-2",
            HashedAccountId = hashedAccountId,
            DriveItemId = "drive-2",
            Name = "Selected2.txt",
            Path = "/Selected2.txt",
            IsSelected = true,
            IsFolder = true
        };

        var unselectedItem = new FileSystemItem
        {
            Id = "unselected-1",
            HashedAccountId = hashedAccountId,
            DriveItemId = "drive-3",
            Name = "Unselected.txt",
            Path = "/Unselected.txt",
            IsSelected = false,
            IsFolder = false
        };

        await _context.FileSystemItems.AddRangeAsync(selectedItem1, selectedItem2, unselectedItem);
        await _context.SaveChangesAsync();

        IEnumerable<FileSystemItem> results = await _repository.GetSelectedItemsByHashedAccountIdAsync(hashedAccountId);

        results.ShouldNotBeNull();
        results.Count().ShouldBe(2);
        results.All(i => i.IsSelected).ShouldBeTrue();
        results.Any(i => i.Id == "selected-1").ShouldBeTrue();
        results.Any(i => i.Id == "selected-2").ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateFileSystemItemPersistsChangesToDatabase()
    {
        var item = new FileSystemItem
        {
            Id = "update-test-id",
            HashedAccountId = "hash-update",
            DriveItemId = "drive-update",
            Name = "Original.txt",
            Path = "/Original.txt",
            IsSelected = false,
            IsFolder = false
        };

        await _context.FileSystemItems.AddAsync(item);
        await _context.SaveChangesAsync();

        item.Name = "Updated.txt";
        item.IsSelected = true;
        await _repository.UpdateAsync(item);

        FileSystemItem? updated = await _context.FileSystemItems.FindAsync(item.Id);
        updated.ShouldNotBeNull();
        updated.Name.ShouldBe("Updated.txt");
        updated.IsSelected.ShouldBeTrue();
    }

    [Fact]
    public async Task DeleteFileSystemItemRemovesFromDatabase()
    {
        var item = new FileSystemItem
        {
            Id = "delete-test-id",
            HashedAccountId = "hash-delete",
            DriveItemId = "drive-delete",
            Name = "ToDelete.txt",
            Path = "/ToDelete.txt",
            IsFolder = false
        };

        await _context.FileSystemItems.AddAsync(item);
        await _context.SaveChangesAsync();

        await _repository.DeleteAsync(item.Id);

        FileSystemItem? deleted = await _context.FileSystemItems.FindAsync(item.Id);
        deleted.ShouldBeNull();
    }

    [Fact]
    public async Task GetAllItemsByHashedAccountIdReturnsAllAccountItems()
    {
        const string hashedAccountId1 = "account-1-hash";
        const string hashedAccountId2 = "account-2-hash";

        var item1 = new FileSystemItem { Id = "item-1", HashedAccountId = hashedAccountId1, DriveItemId = "drive-1", Name = "File1.txt", Path = "/File1.txt", IsFolder = false };
        var item2 = new FileSystemItem { Id = "item-2", HashedAccountId = hashedAccountId1, DriveItemId = "drive-2", Name = "File2.txt", Path = "/File2.txt", IsFolder = false };
        var item3 = new FileSystemItem { Id = "item-3", HashedAccountId = hashedAccountId2, DriveItemId = "drive-3", Name = "File3.txt", Path = "/File3.txt", IsFolder = false };

        await _context.FileSystemItems.AddRangeAsync(item1, item2, item3);
        await _context.SaveChangesAsync();

        IEnumerable<FileSystemItem> results = await _repository.GetAllByHashedAccountIdAsync(hashedAccountId1);

        results.Count().ShouldBe(2);
        results.All(i => i.HashedAccountId == hashedAccountId1).ShouldBeTrue();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}
