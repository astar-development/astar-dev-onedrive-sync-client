# AStar OneDrive Sync Client

A cross-platform OneDrive sync client built with .NET 10, AvaloniaUI, and ReactiveUI. Provides secure multi-account support with bidirectional folder syncing, background sync scheduling, and per-user diagnostic logging.

## Features

- **Multi-Account Support**: Manage multiple Microsoft Personal OneDrive accounts
- **Bidirectional Sync**: Two-way folder synchronization with conflict detection
- **Secure Authentication**: OAuth 2.0 with platform-specific secure token storage
- **Background Scheduling**: Automatic sync at configurable intervals
- **Conflict Resolution**: Interactive UI for handling sync conflicts
- **Diagnostic Logging**: Per-account logging with database-first storage
- **Telemetry**: OpenTelemetry integration for observability

## Prerequisites

- **.NET 10 SDK** or later
- **PostgreSQL 12+** database server
- **Windows/Linux/macOS** (cross-platform support via Avalonia)

## Configuration

The application uses a hierarchical configuration system with the following priority (highest to lowest):

1. Command-line arguments
2. Environment variables
3. User Secrets (development only)
4. `appsettings.{Environment}.json`
5. `appsettings.json`

### Database Setup

1. Install PostgreSQL 12 or later
2. Create the database:

   ```sql
   CREATE DATABASE "astar-dev-onedrive-sync-db";
   CREATE USER "astar-admin" WITH PASSWORD 'your-secure-password';
   GRANT ALL PRIVILEGES ON DATABASE "astar-dev-onedrive-sync-db" TO "astar-admin";
   ```

3. Configure the database password using User Secrets (recommended for development):

   ```bash
   cd src/AStar.Dev.OneDrive.Sync.Client
   dotnet user-secrets set "ConnectionStrings:DatabasePassword" "your-secure-password"
   ```

### Configuration Settings

#### Connection Strings

Located in `appsettings.json` and overridden by User Secrets:

```json
{
  "ConnectionStrings": {
    "OneDriveSync": "Host=localhost;Port=5432;Database=astar-dev-onedrive-sync-db;Username=astar-admin;Password=<from-user-secrets>;Schema=onedrive"
  }
}
```

**User Secret Keys:**

- `ConnectionStrings:DatabasePassword` - PostgreSQL database password (REQUIRED)

#### Authentication Settings

OAuth 2.0 configuration for Microsoft Personal accounts:

> **Note**: Before using the application, you must create an Entra ID (Azure AD) app registration. See the [Entra ID App Registration Guide](docs/guides/entra-id-app-registration.md) for detailed setup instructions.

```json
{
  "Authentication": {
    "Microsoft": {
      "ClientId": "3057f494-687d-4abb-a653-4b8066230b6e",
      "TenantId": "common",
      "RedirectUri": "http://localhost",
      "Scopes": ["Files.ReadWrite", "Files.ReadWrite.All", "offline_access"],
      "LoginTimeout": 30,
      "TokenRefreshMargin": 5
    }
  }
}
```

- `ClientId` - Azure AD application client ID
- `TenantId` - Use "common" for Microsoft Personal accounts
- `RedirectUri` - OAuth redirect URI
- `Scopes` - Required Graph API permissions
- `LoginTimeout` - Login timeout in seconds (default: 30)
- `TokenRefreshMargin` - Minutes before expiry to refresh tokens (default: 5)

**User Secret Keys:**

- `Authentication:Microsoft:ClientSecret` - OAuth client secret (if required by your app registration)

#### Sync Settings

```json
{
  "Sync": {
    "DefaultConcurrentUploads": 5,
    "DefaultConcurrentDownloads": 5,
    "DefaultSyncInterval": 300,
    "ConflictResolutionTimeout": 60,
    "MaxRetryAttempts": 3,
    "RetryBackoffSeconds": 5
  }
}
```

- `DefaultConcurrentUploads` - Maximum parallel upload operations (per account)
- `DefaultConcurrentDownloads` - Maximum parallel download operations (per account)
- `DefaultSyncInterval` - Background sync interval in seconds (default: 300 = 5 minutes)
- `ConflictResolutionTimeout` - Seconds to wait for user resolution before auto-action
- `MaxRetryAttempts` - Number of retry attempts for transient failures
- `RetryBackoffSeconds` - Initial backoff delay for exponential retry

#### Logging Configuration

Serilog configuration for structured logging:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/traces-.json",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 15
        }
      },
      {
        "Name": "PostgreSQL",
        "Args": {
          "connectionString": "OneDriveSync",
          "tableName": "ApplicationLogs",
          "schemaName": "onedrive",
          "needAutoCreateTable": true
        }
      }
    ]
  }
}
```

#### Storage Settings

```json
{
  "Storage": {
    "DefaultSyncDirectory": "%USERPROFILE%\\OneDriveSync",
    "FallbackSecureStorage": true
  }
}
```

- `DefaultSyncDirectory` - Default local sync folder (supports environment variables)
- `FallbackSecureStorage` - Use encrypted file storage if platform-specific storage unavailable

#### Telemetry Settings

```json
{
  "Telemetry": {
    "Enabled": true,
    "ExportToDatabase": true,
    "LogRetentionDays": 15,
    "CriticalLogRetentionDays": 30
  }
}
```

- `Enabled` - Enable telemetry collection
- `ExportToDatabase` - Export logs to PostgreSQL (fallback to file if unavailable)
- `LogRetentionDays` - Days to retain regular logs (default: 15)
- `CriticalLogRetentionDays` - Days to retain critical error logs (default: 30)

### Environment-Specific Configuration

#### Development (`appsettings.Development.json`)

Development overrides enable verbose logging and faster sync intervals:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "Sync": {
    "DefaultSyncInterval": 60
  },
  "Telemetry": {
    "ExportToDatabase": false
  }
}
```

To use development configuration:

```bash
# Windows
$env:ASPNETCORE_ENVIRONMENT="Development"

# Linux/macOS
export ASPNETCORE_ENVIRONMENT=Development
```

### User Secrets Setup

For local development, sensitive configuration should be stored in User Secrets:

```bash
cd src/AStar.Dev.OneDrive.Sync.Client

# Initialize user secrets (already done if you see UserSecretsId in .csproj)
dotnet user-secrets init

# Set database password
dotnet user-secrets set "ConnectionStrings:DatabasePassword" "your-password-here"

# Set OAuth client secret (if needed)
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "your-client-secret"

# List all secrets
dotnet user-secrets list
```

**Important:** User Secrets are stored outside the repository in:

- **Windows**: `%APPDATA%\Microsoft\UserSecrets\<user-secrets-id>\secrets.json`
- **Linux/macOS**: `~/.microsoft/usersecrets/<user-secrets-id>/secrets.json`

### Configuration Best Practices

1. **Never commit secrets** to version control
2. Use User Secrets for local development
3. Use environment variables or secure vaults (Azure Key Vault, AWS Secrets Manager) for production
4. Keep `appsettings.json` with safe defaults and placeholders
5. Use `appsettings.Development.json` for development-specific overrides
6. Document all configuration options in this README

### Sensitive Configuration Summary

The following settings should **NEVER** be committed to the repository:

| Setting                                 | Location     | Description                         |
|-----------------------------------------|--------------|-------------------------------------|
| `ConnectionStrings:DatabasePassword`    | User Secrets | PostgreSQL password                 |
| `Authentication:Microsoft:ClientSecret` | User Secrets | OAuth client secret (if applicable) |

All other configuration can be safely committed to the repository.

## Building and Running

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run (development)
dotnet run --project src/AStar.Dev.OneDrive.Sync.Client

# Run tests
dotnet test
```

## Project Structure

See [PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md) for detailed architecture documentation.

## License

[Specify your license here]

## Contributing

[Specify contribution guidelines here]
