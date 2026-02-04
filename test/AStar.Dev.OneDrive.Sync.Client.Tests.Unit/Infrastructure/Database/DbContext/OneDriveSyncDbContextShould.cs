using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.DbContext;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Database.DbContext;

public class OneDriveSyncDbContextShould
{
    [Fact]
    public void ConfigureOneDriveSchemaAsDefault()
    {
        var options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Schema")
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        var model = context.Model;

        model.GetDefaultSchema().ShouldBe("onedrive");
    }

    [Fact]
    public void ImplementDbContext()
    {
        var options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Implementation")
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        
        context.ShouldNotBeNull();
        context.ShouldBeAssignableTo<Microsoft.EntityFrameworkCore.DbContext>();
    }

    [Fact]
    public void AcceptDbContextOptions()
    {
        var options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Options")
            .Options;

        var act = () => new OneDriveSyncDbContext(options);
        
        act.ShouldNotThrow();
    }

    [Fact]
    public void ProvideAccessToModel()
    {
        var options = new DbContextOptionsBuilder<OneDriveSyncDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_Model")
            .Options;

        using var context = new OneDriveSyncDbContext(options);
        
        context.Model.ShouldNotBeNull();
    }
}