using AStar.Dev.OneDrive.Client.Core.Data;
using AStar.Dev.OneDrive.Client.Core.Models;
using AStar.Dev.OneDrive.Client.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Client.Infrastructure.Tests.Unit.Repositories;

public class SyncConfigurationRepositoryShould
{
    [Fact]
    public async Task GetConfigurationsByAccountIdCorrectly()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        var config1 = new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow);
        var config2 = new SyncConfiguration(0, "acc1", "/Photos", false, DateTime.UtcNow);
        var config3 = new SyncConfiguration(0, "acc2", "/Videos", true, DateTime.UtcNow);
        _ = await repository.AddAsync(config1, TestContext.Current.CancellationToken);
        _ = await repository.AddAsync(config2, TestContext.Current.CancellationToken);
        _ = await repository.AddAsync(config3, TestContext.Current.CancellationToken);

        IReadOnlyList<SyncConfiguration> result = await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result.ShouldContain(c => c.FolderPath == "/Documents");
        result.ShouldContain(c => c.FolderPath == "/Photos");
    }

    [Fact]
    public async Task GetSelectedFoldersOnlyForAccount()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow), TestContext.Current.CancellationToken);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Photos", false, DateTime.UtcNow), TestContext.Current.CancellationToken);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Videos", true, DateTime.UtcNow), TestContext.Current.CancellationToken);

        IReadOnlyList<string> result = await repository.GetSelectedFoldersAsync("acc1", TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result.ShouldContain("/Documents");
        result.ShouldContain("/Videos");
        result.ShouldNotContain("/Photos");
    }

    [Fact]
    public async Task AddConfigurationSuccessfully()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        var config = new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow);

        _ = await repository.AddAsync(config, TestContext.Current.CancellationToken);

        IReadOnlyList<SyncConfiguration> result = await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken);
        result.Count.ShouldBe(1);
        result[0].FolderPath.ShouldBe("/Documents");
        result[0].IsSelected.ShouldBeTrue();
    }

    [Fact]
    public async Task UpdateConfigurationSuccessfully()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        var config = new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow);
        _ = await repository.AddAsync(config, TestContext.Current.CancellationToken);
        SyncConfiguration saved = (await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken))[0];

        var updated = new SyncConfiguration(saved.Id, "acc1", "/Documents", false, DateTime.UtcNow);
        await repository.UpdateAsync(updated, TestContext.Current.CancellationToken);

        SyncConfiguration result = (await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken))[0];
        result.IsSelected.ShouldBeFalse();
    }

    [Fact]
    public async Task ThrowExceptionWhenUpdatingNonExistentConfiguration()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        var config = new SyncConfiguration(999, "acc1", "/Documents", true, DateTime.UtcNow);

        InvalidOperationException exception = await Should.ThrowAsync<InvalidOperationException>(async () => await repository.UpdateAsync(config)
        );

        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task DeleteConfigurationByIdSuccessfully()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow), TestContext.Current.CancellationToken);
        SyncConfiguration saved = (await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken))[0];

        await repository.DeleteAsync(saved.Id, TestContext.Current.CancellationToken);

        IReadOnlyList<SyncConfiguration> result = await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken);
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteAllConfigurationsForAccount()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Documents", true, DateTime.UtcNow), TestContext.Current.CancellationToken);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Photos", false, DateTime.UtcNow), TestContext.Current.CancellationToken);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc2", "/Videos", true, DateTime.UtcNow), TestContext.Current.CancellationToken);

        await repository.DeleteByAccountIdAsync("acc1", TestContext.Current.CancellationToken);

        IReadOnlyList<SyncConfiguration> acc1Result = await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken);
        IReadOnlyList<SyncConfiguration> acc2Result = await repository.GetByAccountIdAsync("acc2", TestContext.Current.CancellationToken);
        acc1Result.ShouldBeEmpty();
        acc2Result.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SaveBatchReplacesExistingConfigurations()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Old1", true, DateTime.UtcNow), TestContext.Current.CancellationToken);
        _ = await repository.AddAsync(new SyncConfiguration(0, "acc1", "/Old2", false, DateTime.UtcNow), TestContext.Current.CancellationToken);

        SyncConfiguration[] newConfigs = [new SyncConfiguration(0, "acc1", "/New1", true, DateTime.UtcNow), new SyncConfiguration(0, "acc1", "/New2", true, DateTime.UtcNow)];
        await repository.SaveBatchAsync("acc1", newConfigs, TestContext.Current.CancellationToken);

        IReadOnlyList<SyncConfiguration> result = await repository.GetByAccountIdAsync("acc1", TestContext.Current.CancellationToken);
        result.Count.ShouldBe(2);
        result.ShouldContain(c => c.FolderPath == "/New1");
        result.ShouldContain(c => c.FolderPath == "/New2");
        result.ShouldNotContain(c => c.FolderPath == "/Old1");
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionForNullAccountId()
    {
        using SyncDbContext context = CreateInMemoryContext();
        var repository = new SyncConfigurationRepository(context);

        _ = await Should.ThrowAsync<ArgumentNullException>(async () => await repository.GetByAccountIdAsync(null!)
        );
    }

    private static SyncDbContext CreateInMemoryContext()
    {
        DbContextOptions<SyncDbContext> options = new DbContextOptionsBuilder<SyncDbContext>()
            .UseInMemoryDatabase(Guid.CreateVersion7().ToString())
            .Options;

        return new SyncDbContext(options);
    }
}
