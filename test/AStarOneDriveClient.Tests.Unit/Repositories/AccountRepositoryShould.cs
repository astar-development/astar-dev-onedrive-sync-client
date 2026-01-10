using AStarOneDriveClient.Data;
using AStarOneDriveClient.Models;
using AStarOneDriveClient.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AStarOneDriveClient.Tests.Unit.Repositories;

public class AccountRepositoryShould
{
    [Fact]
    public async Task ReturnEmptyListWhenNoAccountsExist()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        var result = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task AddNewAccountSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        var account = new AccountInfo("acc1", "John Doe", @"C:\Sync1", true, null, null, false, false, 3, 50, null);

        await repository.AddAsync(account, CancellationToken.None);

        var saved = await repository.GetByIdAsync("acc1", TestContext.Current.CancellationToken);
        saved.ShouldNotBeNull();
        saved.DisplayName.ShouldBe("John Doe");
        saved.LocalSyncPath.ShouldBe(@"C:\Sync1");
        saved.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task GetAllAccountsCorrectly()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        await repository.AddAsync(new AccountInfo("acc1", "User 1", @"C:\Sync1", true, null, null, false, false, 3, 50, null), TestContext.Current.CancellationToken);
        await repository.AddAsync(new AccountInfo("acc2", "User 2", @"C:\Sync2", false, null, null, false, false, 3, 50, null), TestContext.Current.CancellationToken);

        var result = await repository.GetAllAsync(TestContext.Current.CancellationToken);

        result.Count.ShouldBe(2);
        result.ShouldContain(a => a.AccountId == "acc1");
        result.ShouldContain(a => a.AccountId == "acc2");
    }

    [Fact]
    public async Task GetAccountByIdCorrectly()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        await repository.AddAsync(new AccountInfo("acc1", "User 1", @"C:\Sync1", true, null, "token123", false, false, 3, 50, null), TestContext.Current.CancellationToken);

        var result = await repository.GetByIdAsync("acc1", TestContext.Current.CancellationToken);

        result.ShouldNotBeNull();
        result.AccountId.ShouldBe("acc1");
        result.DisplayName.ShouldBe("User 1");
        result.DeltaToken.ShouldBe("token123");
    }

    [Fact]
    public async Task ReturnNullWhenAccountNotFound()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        var result = await repository.GetByIdAsync("nonexistent", TestContext.Current.CancellationToken);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateExistingAccountSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        await repository.AddAsync(new AccountInfo("acc1", "Old Name", @"C:\Sync1", true, null, null, false, false, 3, 50, null), TestContext.Current.CancellationToken);

        var updated = new AccountInfo("acc1", "New Name", @"C:\NewPath", false, DateTime.UtcNow, "newToken", false, false, 3, 50, null);
        await repository.UpdateAsync(updated, TestContext.Current.CancellationToken);

        var result = await repository.GetByIdAsync("acc1", TestContext.Current.CancellationToken);
        result.ShouldNotBeNull();
        result.DisplayName.ShouldBe("New Name");
        result.LocalSyncPath.ShouldBe(@"C:\NewPath");
        result.IsAuthenticated.ShouldBeFalse();
        result.DeltaToken.ShouldBe("newToken");
    }

    [Fact]
    public async Task ThrowExceptionWhenUpdatingNonExistentAccount()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        var account = new AccountInfo("nonexistent", "Name", @"C:\Path", true, null, null, false, false, 3, 50, null);

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await repository.UpdateAsync(account)
        );

        exception.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task DeleteAccountSuccessfully()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        await repository.AddAsync(new AccountInfo("acc1", "User", @"C:\Sync", true, null, null, false, false, 3, 50, null), TestContext.Current.CancellationToken);

        await repository.DeleteAsync("acc1", TestContext.Current.CancellationToken);

        var result = await repository.GetByIdAsync("acc1", TestContext.Current.CancellationToken);
        result.ShouldBeNull();
    }

    [Fact]
    public async Task NotThrowWhenDeletingNonExistentAccount()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        await Should.NotThrowAsync(async () => await repository.DeleteAsync("nonexistent"));
    }

    [Fact]
    public async Task ReturnTrueWhenAccountExists()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);
        await repository.AddAsync(new AccountInfo("acc1", "User", @"C:\Sync", true, null, null, false, false, 3, 50, null), TestContext.Current.CancellationToken);

        var result = await repository.ExistsAsync("acc1", TestContext.Current.CancellationToken);

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnFalseWhenAccountDoesNotExist()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        var result = await repository.ExistsAsync("nonexistent", TestContext.Current.CancellationToken);

        result.ShouldBeFalse();
    }

    [Fact]
    public void ThrowArgumentNullExceptionWhenContextIsNull()
    {
        var exception = Should.Throw<ArgumentNullException>(
            () => new AccountRepository(null!)
        );

        exception.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionWhenAddingNullAccount()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await repository.AddAsync(null!)
        );
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionWhenUpdatingNullAccount()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await repository.UpdateAsync(null!)
        );
    }

    [Fact]
    public async Task ThrowArgumentNullExceptionWhenGettingByNullId()
    {
        using var context = CreateInMemoryContext();
        var repository = new AccountRepository(context);

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await repository.GetByIdAsync(null!)
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
