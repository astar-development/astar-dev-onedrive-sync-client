using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Database.Data;

public class OneDriveSyncDbContextShould
{
    [Fact]
    public void ConfigureOneDriveSchemaAsDefault()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Schema")
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        IModel model = context.Model;

        // SQLite doesn't support schemas, so default schema should be null
        model.GetDefaultSchema().ShouldBeNull();
    }

    [Fact]
    public void ImplementDbContext()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Implementation")
            .Options;

        using var context = new OneDriveSyncDbContext(options);

        context.ShouldNotBeNull();
        context.ShouldBeAssignableTo<DbContext>();
    }

    [Fact]
    public void AcceptDbContextOptions()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Options")
            .Options;

        Func<OneDriveSyncDbContext> act = () => new OneDriveSyncDbContext(options);

        act.ShouldNotThrow();
    }

    [Fact]
    public void ProvideAccessToModel()
    {
        DbContextOptions<OneDriveSyncDbContext> options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Model")
            .Options;

        using var context = new OneDriveSyncDbContext(options);

        context.Model.ShouldNotBeNull();
    }
}
