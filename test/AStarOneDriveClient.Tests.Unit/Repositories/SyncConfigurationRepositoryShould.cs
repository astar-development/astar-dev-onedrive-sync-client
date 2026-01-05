using AStarOneDriveClient.Data;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AStarOneDriveClient.Tests.Unit.Repositories;

public class SyncConfigurationRepositoryShould
{
    [Fact]
    public async Task GetConfigurationsByAccountIdCorrectly()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        var config1 = new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow);
        var config2 = new SyncConfiguration(0, "acc1", "/Photos", false, DateTime.UtcNow);
        var config3 = new SyncConfiguration(0, "acc2", "/Videos", true, DateTime.UtcNow);
        await repository.AddAsync(config1);
        await repository.AddAsync(config2);
        await repository.AddAsync(config3);

        var result = await repository.GetByAccountIdAsync("acc1");

        result.Count.ShouldBe(2);
        result.ShouldContain(c => c.FolderPath == "/Documents");
        result.ShouldContain(c => c.FolderPath == "/Photos");
    }

    [Fact]
    public async Task GetSelectedFoldersOnlyForAccount()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow));
        await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Photos", false, DateTime.UtcNow));
        await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Videos", true, DateTime.UtcNow));

        var result = await repository.GetSelectedFoldersAsync("acc1");

        result.Count.ShouldBe(2);
        result.ShouldContain("/Documents");
        result.ShouldContain("/Videos");
        result.ShouldNotContain("/Photos");
    }

    [Fact]
    public async Task AddConfigurationSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        var config = new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow);

        await repository.AddAsync(config);

        var result = await repository.GetByAccountIdAsync("acc1");
        result.Count.ShouldBe(1);
        result[0].FolderPath.ShouldBe("/Documents");
        result[0].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateConfigurationSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        var config = new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow);
        await repository.AddAsync(config);
        var saved = (await repository.GetByAccountIdAsync("acc1"))[0];

        var updated = new SyncConfiguration(saved.Id, "acc1", "/Documents", false, DateTime.UtcNow);
        await repository.UpdateAsync(updated);

        var result = (await repository.GetByAccountIdAsync("acc1"))[0];
        result.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public async Task ThrowExceptionWhenUpdatingNonExistentConfiguration()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        var config = new SyncConfiguration(999, "acc1", "/Documents", true, DateTime.UtcNow);

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await repository.UpdateAsync(config)
        );

        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task DeleteConfigurationByIdSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow));
        var saved = (await repository.GetByAccountIdAsync("acc1"))[0];

        await repository.DeleteAsync(saved.Id);

        var result = await repository.GetByAccountIdAsync("acc1");
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAllConfigurationsForAccount()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow));
        await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Photos", false, DateTime.UtcNow));
        await repository.AddAsync(new SyncConfiguration(0, "acc2", "/Videos", true, DateTime.UtcNow));

        await repository.DeleteByAccountIdAsync("acc1");

        var acc1Result = await repository.GetByAccountIdAsync("acc1");
        var acc2Result = await repository.GetByAccountIdAsync("acc2");
        acc1Result.ShouldBeEmpty();
        acc2Result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveBatchReplacesExistingConfigurations()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Old1", true, DateTime.UtcNow));
        await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Old2", false, DateTime.UtcNow));

        var newConfigs = new[]
        {
            new SyncConfiguration(0, "acc1", "/New1", true, DateTime.UtcNow),
            new SyncConfiguration(0, "acc1", "/New2", true, DateTime.UtcNow)
        };
        await repository.SaveBatchAsync("acc1", newConfigs);

        var result = await repository.GetByAccountIdAsync("acc1");
        result.Count.ShouldBe(2);
        result.ShouldContain(c => c.FolderPath == "/New1");
        result.ShouldContain(c => c.FolderPath == "/New2");
        result.ShouldNotContain(c => c.FolderPath == "/Old1");
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionForNullAccountId()
    {
        using var context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await repository.GetByAccountIdAsync(null!)
        );
    }

    private static SyncDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new SyncDbContext(options);
    }
}
