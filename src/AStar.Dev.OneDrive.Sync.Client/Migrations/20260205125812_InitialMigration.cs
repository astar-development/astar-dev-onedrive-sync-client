using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    HashedEmail = table.Column<string>(type: "TEXT", nullable: false),
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    HomeSyncDirectory = table.Column<string>(type: "TEXT", nullable: true),
                    MaxConcurrent = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 5),
                    DebugLoggingEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsAdmin = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    MaxBandwidthKBps = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            _ = migrationBuilder.CreateTable(
                name: "DiagnosticSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    LogLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_DiagnosticSettings", x => x.Id);
                });

            _ = migrationBuilder.CreateTable(
                name: "SyncHistory",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    SyncType = table.Column<int>(type: "INTEGER", nullable: false),
                    SyncDirection = table.Column<int>(type: "INTEGER", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SyncResult = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemsUploaded = table.Column<int>(type: "INTEGER", nullable: true),
                    ItemsDownloaded = table.Column<int>(type: "INTEGER", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_SyncHistory", x => x.Id);
                });

            _ = migrationBuilder.CreateTable(
                name: "ApplicationLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "NOW()"),
                    Message = table.Column<string>(type: "TEXT", nullable: true),
                    Exception = table.Column<string>(type: "TEXT", nullable: true),
                    SourceContext = table.Column<string>(type: "TEXT", nullable: true),
                    Properties = table.Column<string>(type: "jsonb", nullable: true),
                    LogLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_ApplicationLogs", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_ApplicationLogs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
                name: "ConflictLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    LocalPath = table.Column<string>(type: "TEXT", nullable: true),
                    LocalLastModified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RemoteLastModified = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolutionAction = table.Column<int>(type: "INTEGER", nullable: false),
                    ConflictType = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_ConflictLogs", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_ConflictLogs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
                name: "DeltaTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    DriveName = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: true),
                    LastSyncAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_DeltaTokens", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_DeltaTokens_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
                name: "FileSystemItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    DriveItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    IsFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                    ParentItemId = table.Column<string>(type: "TEXT", nullable: true),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    LocalPath = table.Column<string>(type: "TEXT", nullable: true),
                    RemoteModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LocalModifiedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RemoteHash = table.Column<string>(type: "TEXT", nullable: true),
                    LocalHash = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSyncDirection = table.Column<int>(type: "INTEGER", nullable: false),
                    AccountId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_FileSystemItems", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_FileSystemItems_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateIndex(
                name: "IX_Accounts_HashedEmail",
                table: "Accounts",
                column: "HashedEmail",
                unique: true);

            _ = migrationBuilder.CreateIndex(
                name: "idx_applicationlogs_hashedaccountid_timestamp",
                table: "ApplicationLogs",
                columns: new[] { "HashedAccountId", "Timestamp" },
                descending: new[] { false, true });

            _ = migrationBuilder.CreateIndex(
                name: "idx_applicationlogs_loglevel",
                table: "ApplicationLogs",
                column: "LogLevel");

            _ = migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogs_AccountId",
                table: "ApplicationLogs",
                column: "AccountId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_ConflictLogs_AccountId",
                table: "ConflictLogs",
                column: "AccountId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_DeltaTokens_AccountId",
                table: "DeltaTokens",
                column: "AccountId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_DiagnosticSettings_HashedAccountId",
                table: "DiagnosticSettings",
                column: "HashedAccountId",
                unique: true);

            _ = migrationBuilder.CreateIndex(
                name: "IX_FileSystemItems_AccountId",
                table: "FileSystemItems",
                column: "AccountId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_FileSystemItems_HashedAccountId",
                table: "FileSystemItems",
                column: "HashedAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropTable(
                name: "ApplicationLogs");

            _ = migrationBuilder.DropTable(
                name: "ConflictLogs");

            _ = migrationBuilder.DropTable(
                name: "DeltaTokens");

            _ = migrationBuilder.DropTable(
                name: "DiagnosticSettings");

            _ = migrationBuilder.DropTable(
                name: "FileSystemItems");

            _ = migrationBuilder.DropTable(
                name: "SyncHistory");

            _ = migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
