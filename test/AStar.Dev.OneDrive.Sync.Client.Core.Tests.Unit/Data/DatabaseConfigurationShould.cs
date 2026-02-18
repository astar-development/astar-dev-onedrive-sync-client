using AStar.Dev.OneDrive.Sync.Client.Core.Data;

namespace AStar.Dev.OneDrive.Sync.Client.Core.Tests.Unit.Data;

public class DatabaseConfigurationShould
{
    [Fact]
    public void ReturnValidDatabasePath()
    {
        var databasePath = DatabaseConfiguration.DatabasePath;

        databasePath.ShouldNotBeNullOrEmpty();
        databasePath.ShouldEndWith("sync.db");
    }

    [Fact]
    public void ReturnValidConnectionString()
    {
        var connectionString = DatabaseConfiguration.ConnectionString;

        connectionString.ShouldNotBeNullOrEmpty();
        connectionString.ShouldStartWith("Data Source=");
        connectionString.ShouldContain(DatabaseConfiguration.DatabasePath);
    }
}
