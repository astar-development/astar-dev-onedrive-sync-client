using AStar.Dev.OneDrive.Sync.Client.Core;
using AStar.Dev.OneDrive.Sync.Client.Core.Data.Entities;
using AStar.Dev.OneDrive.Sync.Client.Core.Models;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Tests.Unit.Repositories;

public class SyncConfigurationRepositoryShould
{
    private readonly IDbContextFactory<SyncDbContext> _contextFactory;

    public SyncConfigurationRepositoryShould() => _contextFactory = new PooledDbContextFactory<SyncDbContext>(
        new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public async Task UpdateExistingRootFolderRelativePathWhenNodePathDiffers()
    {
        var accountId = new HashedAccountId(AccountIdHasher.Hash("acc1"));
        var repository = new SyncConfigurationRepository(_contextFactory);
        await repository.AddAsync(
            new FileMetadata("folder-1", accountId, "OldName", "/OldPath", 0, DateTimeOffset.UtcNow, "", IsFolder: true),
            TestContext.Current.CancellationToken);

        var updatedNode = new OneDriveFolderNode("folder-1", "NewName", "/NewPath", null, true);

        await repository.UpdateFoldersByAccountIdAsync(accountId, [updatedNode], TestContext.Current.CancellationToken);

        IReadOnlyList<FileMetadata> result = await repository.GetByAccountIdAsync(accountId, TestContext.Current.CancellationToken);
        FileMetadata? updated = result.SingleOrDefault(r => r.DriveItemId == "folder-1");
        _ = updated.ShouldNotBeNull();
        updated.RelativePath.ShouldBe("/NewPath");
        updated.Name.ShouldBe("NewName");
    }

    [Fact]
    public async Task UpdateExistingChildFolderRelativePathWhenNodePathDiffers()
    {
        var accountId = new HashedAccountId(AccountIdHasher.Hash("acc2"));
        var repository = new SyncConfigurationRepository(_contextFactory);
        await repository.AddAsync(
            new FileMetadata("child-1", accountId, "OldChild", "/OldChildPath", 0, DateTimeOffset.UtcNow, "", IsFolder: true),
            TestContext.Current.CancellationToken);

        var rootNode = new OneDriveFolderNode("root-1", "Root", "/Root", null, true);
        rootNode.Children.Add(new OneDriveFolderNode("child-1", "UpdatedChild", "/Root/UpdatedChild", "root-1", true));

        await repository.UpdateFoldersByAccountIdAsync(accountId, [rootNode], TestContext.Current.CancellationToken);

        IReadOnlyList<FileMetadata> result = await repository.GetByAccountIdAsync(accountId, TestContext.Current.CancellationToken);
        FileMetadata? child = result.SingleOrDefault(r => r.DriveItemId == "child-1");
        _ = child.ShouldNotBeNull();
        child.RelativePath.ShouldBe("/Root/UpdatedChild");
        child.Name.ShouldBe("UpdatedChild");
    }

    [Fact(Skip ="Will fix during rewrite of core functionality")]
    public async Task NotAddRootFolderWhenNotPresentInDatabase()
    {
        var accountId = new HashedAccountId(AccountIdHasher.Hash("acc3"));
        var repository = new SyncConfigurationRepository(_contextFactory);
        var rootNode = new OneDriveFolderNode("root-new", "NewRoot", "/NewRoot", null, true);

        await repository.UpdateFoldersByAccountIdAsync(accountId, [rootNode], TestContext.Current.CancellationToken);

        IReadOnlyList<FileMetadata> result = await repository.GetByAccountIdAsync(accountId, TestContext.Current.CancellationToken);
        result.ShouldBeEmpty();
    }

    [Fact(Skip ="Will fix during rewrite of core functionality")]
    public async Task ReturnOnlyFirstTwoLevelFoldersWhenQueryingFoldersByAccountId()
    {
        var accountId = new HashedAccountId(AccountIdHasher.Hash("depth-test-acc"));
        var repository = new SyncConfigurationRepository(_contextFactory);

        await repository.AddAsync(new FileMetadata("folder-l1", accountId, "Documents", "/Documents", 0, DateTimeOffset.UtcNow, "", IsFolder: true), TestContext.Current.CancellationToken);
        await repository.AddAsync(new FileMetadata("folder-l2", accountId, "Work", "/Documents/Work", 0, DateTimeOffset.UtcNow, "", IsFolder: true), TestContext.Current.CancellationToken);
        await repository.AddAsync(new FileMetadata("folder-l3", accountId, "Projects", "/Documents/Work/Projects", 0, DateTimeOffset.UtcNow, "", IsFolder: true), TestContext.Current.CancellationToken);

        IReadOnlyList<DriveItemEntity> result = await repository.GetFoldersByAccountIdAsync(accountId, TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result.ShouldContain(f => f.DriveItemId == "folder-l1");
        result.ShouldContain(f => f.DriveItemId == "folder-l2");
        result.ShouldNotContain(f => f.DriveItemId == "folder-l3");
    }

    [Fact]
    public async Task AddChildFolderWhenNotPresentInDatabase()
    {
        var accountId = new HashedAccountId(AccountIdHasher.Hash("acc4"));
        var repository = new SyncConfigurationRepository(_contextFactory);
        var rootNode = new OneDriveFolderNode("root-1", "Root", "/Root", null, true);
        rootNode.Children.Add(new OneDriveFolderNode("child-new", "NewChild", "/Root/NewChild", "root-1", true, false));

        await repository.UpdateFoldersByAccountIdAsync(accountId, [rootNode], TestContext.Current.CancellationToken);

        IReadOnlyList<FileMetadata> result = await repository.GetByAccountIdAsync(accountId, TestContext.Current.CancellationToken);
        FileMetadata? child = result.SingleOrDefault(r => r.DriveItemId == "child-new");
        _ = child.ShouldNotBeNull();
        child.RelativePath.ShouldBe("/Root/NewChild");
        child.IsFolder.ShouldBeTrue();
        child.IsSelected.ShouldBeFalse();
    }
}
