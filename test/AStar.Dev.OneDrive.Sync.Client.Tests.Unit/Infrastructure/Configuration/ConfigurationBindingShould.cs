using AStar.Dev.OneDrive.Sync.Client.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace AStar.Dev.OneDrive.Sync.Client.Tests.Unit.Infrastructure.Configuration;

public class ConfigurationBindingShould
{
    public class AuthenticationConfiguration
    {
        [Fact]
        public void BindAuthenticationOptionsFromConfiguration()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Authentication:Microsoft:ClientId"] = "test-client-id",
                ["Authentication:Microsoft:TenantId"] = "test-tenant",
                ["Authentication:Microsoft:RedirectUri"] = "http://test-redirect",
                ["Authentication:Microsoft:Scopes:0"] = "Files.ReadWrite",
                ["Authentication:Microsoft:Scopes:1"] = "offline_access",
                ["Authentication:Microsoft:LoginTimeout"] = "45",
                ["Authentication:Microsoft:TokenRefreshMargin"] = "10"
            });

            var options = configuration
                .GetSection(AuthenticationOptions.SectionName)
                .Get<AuthenticationOptions>();

            options.ShouldNotBeNull();
            options.Microsoft.ClientId.ShouldBe("test-client-id");
            options.Microsoft.TenantId.ShouldBe("test-tenant");
            options.Microsoft.RedirectUri.ShouldBe("http://test-redirect");
            options.Microsoft.Scopes.ShouldBe(["Files.ReadWrite", "offline_access"]);
            options.Microsoft.LoginTimeout.ShouldBe(45);
            options.Microsoft.TokenRefreshMargin.ShouldBe(10);
        }

        [Fact]
        public void UseDefaultValuesWhenNotConfigured()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>());

            var options = configuration
                .GetSection(AuthenticationOptions.SectionName)
                .Get<AuthenticationOptions>() ?? new AuthenticationOptions();

            options.Microsoft.ClientId.ShouldBe(string.Empty);
            options.Microsoft.LoginTimeout.ShouldBe(30);
            options.Microsoft.TokenRefreshMargin.ShouldBe(5);
            options.Microsoft.ClientSecret.ShouldBeNull();
        }

        [Fact]
        public void BindClientSecretFromUserSecretsOrEnvironment()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Authentication:Microsoft:ClientId"] = "test-client-id",
                ["Authentication:Microsoft:ClientSecret"] = "secret-from-user-secrets"
            });

            var options = configuration
                .GetSection(AuthenticationOptions.SectionName)
                .Get<AuthenticationOptions>();

            options.ShouldNotBeNull();
            options.Microsoft.ClientSecret.ShouldBe("secret-from-user-secrets");
        }
    }

    public class SyncConfiguration
    {
        [Fact]
        public void BindSyncOptionsFromConfiguration()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Sync:DefaultConcurrentUploads"] = "10",
                ["Sync:DefaultConcurrentDownloads"] = "8",
                ["Sync:DefaultSyncInterval"] = "600",
                ["Sync:ConflictResolutionTimeout"] = "120",
                ["Sync:MaxRetryAttempts"] = "5",
                ["Sync:RetryBackoffSeconds"] = "10"
            });

            var options = configuration
                .GetSection(SyncOptions.SectionName)
                .Get<SyncOptions>();

            options.ShouldNotBeNull();
            options.DefaultConcurrentUploads.ShouldBe(10);
            options.DefaultConcurrentDownloads.ShouldBe(8);
            options.DefaultSyncInterval.ShouldBe(600);
            options.ConflictResolutionTimeout.ShouldBe(120);
            options.MaxRetryAttempts.ShouldBe(5);
            options.RetryBackoffSeconds.ShouldBe(10);
        }

        [Fact]
        public void UseDefaultValuesWhenNotConfigured()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>());

            var options = configuration
                .GetSection(SyncOptions.SectionName)
                .Get<SyncOptions>() ?? new SyncOptions();

            options.DefaultConcurrentUploads.ShouldBe(5);
            options.DefaultConcurrentDownloads.ShouldBe(5);
            options.DefaultSyncInterval.ShouldBe(300);
            options.ConflictResolutionTimeout.ShouldBe(60);
            options.MaxRetryAttempts.ShouldBe(3);
            options.RetryBackoffSeconds.ShouldBe(5);
        }
    }

    public class StorageConfiguration
    {
        [Fact]
        public void BindStorageOptionsFromConfiguration()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Storage:DefaultSyncDirectory"] = "C:\\CustomSync",
                ["Storage:FallbackSecureStorage"] = "false"
            });

            var options = configuration
                .GetSection(StorageOptions.SectionName)
                .Get<StorageOptions>();

            options.ShouldNotBeNull();
            options.DefaultSyncDirectory.ShouldBe("C:\\CustomSync");
            options.FallbackSecureStorage.ShouldBeFalse();
        }

        [Fact]
        public void UseDefaultValuesWhenNotConfigured()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>());

            var options = configuration
                .GetSection(StorageOptions.SectionName)
                .Get<StorageOptions>() ?? new StorageOptions();

            options.DefaultSyncDirectory.ShouldBe("%USERPROFILE%\\OneDriveSync");
            options.FallbackSecureStorage.ShouldBeTrue();
        }
    }

    public class TelemetryConfiguration
    {
        [Fact]
        public void BindTelemetryOptionsFromConfiguration()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Telemetry:Enabled"] = "false",
                ["Telemetry:ExportToDatabase"] = "false",
                ["Telemetry:LogRetentionDays"] = "30",
                ["Telemetry:CriticalLogRetentionDays"] = "60"
            });

            var options = configuration
                .GetSection(TelemetryOptions.SectionName)
                .Get<TelemetryOptions>();

            options.ShouldNotBeNull();
            options.Enabled.ShouldBeFalse();
            options.ExportToDatabase.ShouldBeFalse();
            options.LogRetentionDays.ShouldBe(30);
            options.CriticalLogRetentionDays.ShouldBe(60);
        }

        [Fact]
        public void UseDefaultValuesWhenNotConfigured()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>());

            var options = configuration
                .GetSection(TelemetryOptions.SectionName)
                .Get<TelemetryOptions>() ?? new TelemetryOptions();

            options.Enabled.ShouldBeTrue();
            options.ExportToDatabase.ShouldBeTrue();
            options.LogRetentionDays.ShouldBe(15);
            options.CriticalLogRetentionDays.ShouldBe(30);
        }
    }

    public class ConnectionStringConfiguration
    {
        [Fact]
        public void ReadConnectionStringFromConfiguration()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OneDriveSync"] = "Host=localhost;Database=test-db;Username=test-user;Password=test-pwd"
            });

            var connectionString = configuration.GetConnectionString("OneDriveSync");

            connectionString.ShouldNotBeNull();
            connectionString.ShouldContain("Host=localhost");
            connectionString.ShouldContain("Database=test-db");
        }

        [Fact]
        public void ReplacePasswordPlaceholderWithActualPassword()
        {
            var configuration = BuildConfiguration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:OneDriveSync"] = "Host=localhost;Password=<from-user-secrets>",
                ["ConnectionStrings:DatabasePassword"] = "actual-secret-password"
            });

            var connectionString = configuration.GetConnectionString("OneDriveSync");
            var password = configuration["ConnectionStrings:DatabasePassword"];

            connectionString.ShouldNotBeNull();
            password.ShouldBe("actual-secret-password");
        }
    }

    public class EnvironmentSpecificConfiguration
    {
        [Fact]
        public void DevelopmentOverridesProductionSettings()
        {
            var productionConfig = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Sync:DefaultSyncInterval"] = "300"
            });

            var devConfig = BuildConfiguration(new Dictionary<string, string?>
            {
                ["Sync:DefaultSyncInterval"] = "60"
            });

            var prodOptions = productionConfig
                .GetSection(SyncOptions.SectionName)
                .Get<SyncOptions>();

            var devOptions = devConfig
                .GetSection(SyncOptions.SectionName)
                .Get<SyncOptions>();

            prodOptions.ShouldNotBeNull();
            devOptions.ShouldNotBeNull();
            prodOptions.DefaultSyncInterval.ShouldBe(300);
            devOptions.DefaultSyncInterval.ShouldBe(60);
        }
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> initialData)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(initialData)
            .Build();
    }
}
