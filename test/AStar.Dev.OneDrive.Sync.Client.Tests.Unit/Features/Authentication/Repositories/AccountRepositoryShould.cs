using AStar.Dev.OneDrive.Sync.Client.Common.Models;
using AStar.Dev.OneDrive.Sync.Client.Features.Authentication.Repositories;
using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data;
using Microsoft.EntityFrameworkCore;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Features.Authentication.Repositories;
public class AccountRepositoryShould
{
    [Fact]
    public async Task CreateAccountAndPersistToDatabase()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        var account = new Account
        {
            HashedEmail = "hashed.email@example.com",
            HashedAccountId = "hashed.account.id",
            HomeSyncDirectory = "/home/user/sync",
            MaxConcurrent = 5,
            IsAdmin = false
        };

        await repository.CreateAsync(account);
        Account? retrievedAccount = await repository.GetByIdAsync(account.Id);

        retrievedAccount.ShouldNotBeNull();
        retrievedAccount.HashedEmail.ShouldBe(account.HashedEmail);
        retrievedAccount.HashedAccountId.ShouldBe(account.HashedAccountId);
    }

    [Fact]
    public async Task GetAccountByIdReturnsAccountWhenExists()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        var account = new Account { HashedEmail = "test1@example.com", HashedAccountId = "id1" };
        await repository.CreateAsync(account);

        Account? retrieved = await repository.GetByIdAsync(account.Id);

        retrieved.ShouldNotBeNull();
        retrieved.Id.ShouldBe(account.Id);
    }

    [Fact]
    public async Task GetAccountByIdReturnsNullWhenNotExists()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        Account? retrieved = await repository.GetByIdAsync(Guid.NewGuid());

        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task GetByHashedEmailReturnsAccountWhenExists()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        const string hashedEmail = "unique.hash@email.com";
        var account = new Account { HashedEmail = hashedEmail, HashedAccountId = "id2" };
        await repository.CreateAsync(account);

        Account? retrieved = await repository.GetByHashedEmailAsync(hashedEmail);

        retrieved.ShouldNotBeNull();
        retrieved.HashedEmail.ShouldBe(hashedEmail);
    }

    [Fact]
    public async Task GetByHashedEmailReturnsNullWhenNotExists()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        Account? retrieved = await repository.GetByHashedEmailAsync("nonexistent@email.com");

        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task GetByHashedAccountIdReturnsAccountWhenExists()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        const string hashedAccountId = "hashed-account-id-123";
        var account = new Account { HashedEmail = "test@example.com", HashedAccountId = hashedAccountId };
        await repository.CreateAsync(account);

        Account? retrieved = await repository.GetByHashedAccountIdAsync(hashedAccountId);

        retrieved.ShouldNotBeNull();
        retrieved.HashedAccountId.ShouldBe(hashedAccountId);
    }

    [Fact]
    public async Task GetByHashedAccountIdReturnsNullWhenNotExists()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        Account? retrieved = await repository.GetByHashedAccountIdAsync("nonexistent-id");

        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateAccountPersistsChangesToDatabase()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        var account = new Account { HashedEmail = "test2@example.com", HashedAccountId = "id3" };
        await repository.CreateAsync(account);

        account.HomeSyncDirectory = "/new/sync/path";
        account.MaxConcurrent = 10;

        await repository.UpdateAsync(account);

        using var verifyContext = new OneDriveSyncDbContext(options);
        Account? retrieved = await verifyContext.Accounts.FindAsync(account.Id);

        retrieved.ShouldNotBeNull();
        retrieved.HomeSyncDirectory.ShouldBe("/new/sync/path");
        retrieved.MaxConcurrent.ShouldBe(10);
    }

    [Fact]
    public async Task DeleteAccountRemovesItFromDatabase()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        var account = new Account { HashedEmail = "test4@example.com", HashedAccountId = "id5" };
        await repository.CreateAsync(account);
        Guid accountId = account.Id;

        await repository.DeleteAsync(accountId);

        using var verifyContext = new OneDriveSyncDbContext(options);
        Account? retrieved = await verifyContext.Accounts.FindAsync(accountId);

        retrieved.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteNonexistentAccountDoesNotThrow()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        Func<Task> act = async () => await repository.DeleteAsync(Guid.NewGuid());

        await act.ShouldNotThrowAsync();
    }

    [Fact]
    public async Task GetAllAccountsReturnsAllAccounts()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        var account1 = new Account { HashedEmail = "test5@example.com", HashedAccountId = "id6" };
        var account2 = new Account { HashedEmail = "test6@example.com", HashedAccountId = "id7" };
        var account3 = new Account { HashedEmail = "test7@example.com", HashedAccountId = "id8" };

        await repository.CreateAsync(account1);
        await repository.CreateAsync(account2);
        await repository.CreateAsync(account3);

        var allAccounts = (await repository.GetAllAsync()).ToList();

        allAccounts.Count.ShouldBe(3);
        allAccounts.ShouldContain(a => a.Id == account1.Id);
        allAccounts.ShouldContain(a => a.Id == account2.Id);
        allAccounts.ShouldContain(a => a.Id == account3.Id);
        allAccounts.ShouldContain(a => a.Id == account1.Id);
        allAccounts.ShouldContain(a => a.Id == account2.Id);
        allAccounts.ShouldContain(a => a.Id == account3.Id);
    }

    [Fact]
    public async Task GetAllAccountsReturnsEmptyListWhenNoAccountsExist()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        var allAccounts = (await repository.GetAllAsync()).ToList();

        allAccounts.ShouldBeEmpty();
    }

    [Fact]
    public async Task IsEmailHashUniqueReturnsTrueWhenHashDoesNotExist()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        var isUnique = await repository.IsEmailHashUniqueAsync("unique.hash@email.com");

        isUnique.ShouldBeTrue();
    }

    [Fact]
    public async Task IsEmailHashUniqueReturnsFalseWhenHashExists()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        var existingAccount = new Account { HashedEmail = "existing.hash@email.com", HashedAccountId = "id9" };
        await repository.CreateAsync(existingAccount);

        var isUnique = await repository.IsEmailHashUniqueAsync("existing.hash@email.com");

        isUnique.ShouldBeFalse();
    }

    [Fact]
    public async Task GetAdminAccountsReturnsOnlyAdminAccounts()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        var adminAccount1 = new Account { HashedEmail = "admin1@example.com", HashedAccountId = "id10", IsAdmin = true };
        var adminAccount2 = new Account { HashedEmail = "admin2@example.com", HashedAccountId = "id11", IsAdmin = true };
        var userAccount = new Account { HashedEmail = "user1@example.com", HashedAccountId = "id12", IsAdmin = false };

        await repository.CreateAsync(adminAccount1);
        await repository.CreateAsync(adminAccount2);
        await repository.CreateAsync(userAccount);

        var adminAccounts = (await repository.GetAdminAccountsAsync()).ToList();

        adminAccounts.Count.ShouldBe(2);
        adminAccounts.ShouldAllBe(a => a.IsAdmin);
        adminAccounts.ShouldContain(a => a.Id == adminAccount1.Id);
        adminAccounts.ShouldContain(a => a.Id == adminAccount2.Id);
    }

    [Fact]
    public async Task DoesAccountExistReturnsTrueWhenIdExists()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        var account = new Account { HashedEmail = "test8@example.com", HashedAccountId = "id13" };
        await repository.CreateAsync(account);

        var exists = await repository.DoesAccountExistAsync(account.Id);

        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task DoesAccountExistReturnsFalseWhenIdDoesNotExist()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var repository = new AccountRepository(context);

        var exists = await repository.DoesAccountExistAsync(Guid.NewGuid());

        exists.ShouldBeFalse();
    }
}


