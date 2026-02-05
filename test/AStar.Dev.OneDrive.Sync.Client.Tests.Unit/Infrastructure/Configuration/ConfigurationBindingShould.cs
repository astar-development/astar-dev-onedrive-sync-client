using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Configuration;

public class ConfigurationBindingShould
{
    private readonly string _testProjectPath;

    public ConfigurationBindingShould()
    {
        var testAssemblyPath = Directory.GetCurrentDirectory();
        _testProjectPath = Path.GetFullPath(Path.Combine(
            testAssemblyPath,
            "..", "..", "..", "..", "..",
            "src", "AStar.Dev.OneDrive.Sync.Client"));
    }

    public class ProductionConfigurationLoading : ConfigurationBindingShould
    {
        [Fact]
        public void LoadAppsettingsJsonFromProduction()
        {
            IConfiguration configuration = ConfigurationFactory.Build([], _testProjectPath);

            configuration.ShouldNotBeNull();
            var connectionString = configuration.GetConnectionString("OneDriveSync");
            connectionString.ShouldNotBeNull();
            connectionString.ShouldContain("Data Source=");
            connectionString.ShouldContain("onedrive-sync.db");
        }

        [Fact]
        public void LoadAuthenticationSettingsFromAppsettingsJson()
        {
            IConfiguration configuration = ConfigurationFactory.Build([], _testProjectPath);

            AuthenticationOptions? authOptions = configuration
                .GetSection(AuthenticationOptions.SectionName)
                .Get<AuthenticationOptions>();

            authOptions.ShouldNotBeNull();
            authOptions.Microsoft.ClientId.ShouldBe("3057f494-687d-4abb-a653-4b8066230b6e");
            authOptions.Microsoft.TenantId.ShouldBe("common");
            authOptions.Microsoft.RedirectUri.ShouldBe("http://localhost");
            authOptions.Microsoft.Scopes.ShouldContain("Files.ReadWrite");
            authOptions.Microsoft.Scopes.ShouldContain("offline_access");
            authOptions.Microsoft.LoginTimeout.ShouldBe(30);
            authOptions.Microsoft.TokenRefreshMargin.ShouldBe(5);
        }

        [Fact]
        public void LoadSyncSettingsFromAppsettingsJson()
        {
            IConfiguration configuration = ConfigurationFactory.Build([], _testProjectPath);

            SyncOptions? syncOptions = configuration
                .GetSection(SyncOptions.SectionName)
                .Get<SyncOptions>();

            syncOptions.ShouldNotBeNull();
            syncOptions.DefaultConcurrentUploads.ShouldBe(5);
            syncOptions.DefaultConcurrentDownloads.ShouldBe(5);
            syncOptions.DefaultSyncInterval.ShouldBe(300);
            syncOptions.ConflictResolutionTimeout.ShouldBe(60);
            syncOptions.MaxRetryAttempts.ShouldBe(3);
            syncOptions.RetryBackoffSeconds.ShouldBe(5);
        }

        [Fact]
        public void LoadStorageSettingsFromAppsettingsJson()
        {
            IConfiguration configuration = ConfigurationFactory.Build([], _testProjectPath);

            StorageOptions? storageOptions = configuration
                .GetSection(StorageOptions.SectionName)
                .Get<StorageOptions>();

            storageOptions.ShouldNotBeNull();
            storageOptions.DefaultSyncDirectory.ShouldBe("%USERPROFILE%\\OneDriveSync");
            storageOptions.FallbackSecureStorage.ShouldBeTrue();
        }

        [Fact]
        public void LoadTelemetrySettingsFromAppsettingsJson()
        {
            IConfiguration configuration = ConfigurationFactory.Build([], _testProjectPath);

            TelemetryOptions? telemetryOptions = configuration
                .GetSection(TelemetryOptions.SectionName)
                .Get<TelemetryOptions>();

            telemetryOptions.ShouldNotBeNull();
            telemetryOptions.Enabled.ShouldBeTrue();
            telemetryOptions.ExportToDatabase.ShouldBeTrue();
            telemetryOptions.LogRetentionDays.ShouldBe(15);
            telemetryOptions.CriticalLogRetentionDays.ShouldBe(30);
        }
    }

    public class EnvironmentSpecificConfiguration : ConfigurationBindingShould
    {
        [Fact]
        public void LoadDevelopmentOverridesWhenEnvironmentIsDevelopment()
        {
            IConfiguration configuration = ConfigurationFactory.Build([], _testProjectPath, "Development");

            SyncOptions? syncOptions = configuration
                .GetSection(SyncOptions.SectionName)
                .Get<SyncOptions>();

            syncOptions.ShouldNotBeNull();
            syncOptions.DefaultSyncInterval.ShouldBe(60);
            syncOptions.MaxRetryAttempts.ShouldBe(5);
        }

        [Fact]
        public void DevelopmentConfigurationEnablesVerboseLogging()
        {
            IConfiguration configuration = ConfigurationFactory.Build([], _testProjectPath, "Development");

            var logLevel = configuration["Logging:LogLevel:Default"];
            logLevel.ShouldBe("Debug");
        }

        [Fact]
        public void DevelopmentConfigurationDisablesDatabaseTelemetry()
        {
            IConfiguration configuration = ConfigurationFactory.Build([], _testProjectPath, "Development");

            TelemetryOptions? telemetryOptions = configuration
                .GetSection(TelemetryOptions.SectionName)
                .Get<TelemetryOptions>();

            telemetryOptions.ShouldNotBeNull();
            telemetryOptions.ExportToDatabase.ShouldBeFalse();
        }
    }

    public class CommandLineOverrides : ConfigurationBindingShould
    {
        [Fact]
        public void CommandLineArgumentsOverrideJsonSettings()
        {
            var args = new[]
            {
                "--Sync:DefaultSyncInterval=999",
                "--Sync:MaxRetryAttempts=10"
            };

            IConfiguration configuration = ConfigurationFactory.Build(args, _testProjectPath);

            SyncOptions? syncOptions = configuration
                .GetSection(SyncOptions.SectionName)
                .Get<SyncOptions>();

            syncOptions.ShouldNotBeNull();
            syncOptions.DefaultSyncInterval.ShouldBe(999);
            syncOptions.MaxRetryAttempts.ShouldBe(10);
        }
    }

    [Collection("EnvironmentVariableTests")]
    public class EnvironmentVariableOverrides : ConfigurationBindingShould, IDisposable
    {
        public EnvironmentVariableOverrides()
        {
            Environment.SetEnvironmentVariable("Sync__DefaultSyncInterval", null);
            Environment.SetEnvironmentVariable("Telemetry__Enabled", null);
        }

        [Fact]
        public void EnvironmentVariablesOverrideJsonSettings()
        {
            Environment.SetEnvironmentVariable("Sync__DefaultSyncInterval", "1234");
            Environment.SetEnvironmentVariable("Telemetry__Enabled", "false");

            IConfiguration configuration = ConfigurationFactory.Build([], _testProjectPath);

            SyncOptions? syncOptions = configuration
                .GetSection(SyncOptions.SectionName)
                .Get<SyncOptions>();

            TelemetryOptions? telemetryOptions = configuration
                .GetSection(TelemetryOptions.SectionName)
                .Get<TelemetryOptions>();

            syncOptions.ShouldNotBeNull();
            syncOptions.DefaultSyncInterval.ShouldBe(1234);

            telemetryOptions.ShouldNotBeNull();
            telemetryOptions.Enabled.ShouldBeFalse();
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("Sync__DefaultSyncInterval", null);
            Environment.SetEnvironmentVariable("Telemetry__Enabled", null);
        }
    }

    [Collection("EnvironmentVariableTests")]
    public class ConfigurationHierarchy : ConfigurationBindingShould, IDisposable
    {
        public ConfigurationHierarchy() => Environment.SetEnvironmentVariable("Sync__DefaultSyncInterval", null);

        [Fact]
        public void CommandLineOverridesEnvironmentVariables()
        {
            Environment.SetEnvironmentVariable("Sync__DefaultSyncInterval", "888");

            var args = new[] { "--Sync:DefaultSyncInterval=777" };
            IConfiguration configuration = ConfigurationFactory.Build(args, _testProjectPath);

            SyncOptions? syncOptions = configuration
                .GetSection(SyncOptions.SectionName)
                .Get<SyncOptions>();

            syncOptions.ShouldNotBeNull();
            syncOptions.DefaultSyncInterval.ShouldBe(777);
        }

        [Fact]
        public void EnvironmentVariablesOverrideJsonFiles()
        {
            Environment.SetEnvironmentVariable("Sync__DefaultSyncInterval", "555");

            IConfiguration configuration = ConfigurationFactory.Build([], _testProjectPath);

            SyncOptions? syncOptions = configuration
                .GetSection(SyncOptions.SectionName)
                .Get<SyncOptions>();

            syncOptions.ShouldNotBeNull();
            syncOptions.DefaultSyncInterval.ShouldBe(555);
        }

        public void Dispose() => Environment.SetEnvironmentVariable("Sync__DefaultSyncInterval", null);
    }

    public class SerilogConfiguration : ConfigurationBindingShould
    {
        [Fact]
        public void LoadSerilogSinkConfigurationFromAppsettings()
        {
            IConfiguration configuration = ConfigurationFactory.Build([], _testProjectPath);

            var sinkNames = configuration.GetSection("Serilog:WriteTo")
                .GetChildren()
                .Select(c => c["Name"])
                .ToList();

            sinkNames.ShouldContain("Console");
            sinkNames.ShouldContain("File");
            // SQLite migration: PostgreSQL sink removed
            sinkNames.ShouldNotContain("PostgreSQL");
        }

        [Fact]
        public void LoadSerilogMinimumLevelFromAppsettings()
        {
            IConfiguration configuration = ConfigurationFactory.Build([], _testProjectPath);

            var minLevel = configuration["Serilog:MinimumLevel:Default"];
            minLevel.ShouldBe("Information");
        }
    }
}
