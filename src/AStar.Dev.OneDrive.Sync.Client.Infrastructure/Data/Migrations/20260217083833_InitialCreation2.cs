using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreation2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    Id = table.Column<string>(type: "TEXT", nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    LocalSyncPath = table.Column<string>(type: "TEXT", nullable: false),
                    IsAuthenticated = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSyncUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeltaToken = table.Column<string>(type: "TEXT", nullable: true),
                    EnableDetailedSyncLogging = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableDebugLogging = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxParallelUpDownloads = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxItemsInBatch = table.Column<int>(type: "INTEGER", nullable: false),
                    AutoSyncIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_Accounts", x => x.HashedAccountId);
                });

            _ = migrationBuilder.CreateTable(
                name: "WindowPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    X = table.Column<double>(type: "REAL", nullable: true),
                    Y = table.Column<double>(type: "REAL", nullable: true),
                    Width = table.Column<double>(type: "REAL", nullable: false, defaultValue: 800.0),
                    Height = table.Column<double>(type: "REAL", nullable: false, defaultValue: 600.0),
                    IsMaximized = table.Column<bool>(type: "INTEGER", nullable: false),
                    Theme = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_WindowPreferences", x => x.Id);
                });

            _ = migrationBuilder.CreateTable(
                name: "DebugLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    TimestampUtc_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    LogLevel = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Exception = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_DebugLogs", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_DebugLogs_Accounts_HashedAccountId",
                        column: x => x.HashedAccountId,
                        principalTable: "Accounts",
                        principalColumn: "HashedAccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
                name: "DeltaTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    LastSyncedUtc_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_DeltaTokens", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_DeltaTokens_Accounts_HashedAccountId",
                        column: x => x.HashedAccountId,
                        principalTable: "Accounts",
                        principalColumn: "HashedAccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
                name: "DriveItems",
                columns: table => new
                {
                    DriveItemId = table.Column<string>(type: "TEXT", nullable: false),
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    ETag = table.Column<string>(type: "TEXT", nullable: true),
                    CTag = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    LastModifiedUtc_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    IsFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: true),
                    RemoteHash = table.Column<string>(type: "TEXT", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: true),
                    LocalPath = table.Column<string>(type: "TEXT", nullable: true),
                    LocalHash = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSyncDirection = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_DriveItems", x => x.DriveItemId);
                    _ = table.ForeignKey(
                        name: "FK_DriveItems_Accounts_HashedAccountId",
                        column: x => x.HashedAccountId,
                        principalTable: "Accounts",
                        principalColumn: "HashedAccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
                name: "FileOperationLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SyncSessionId = table.Column<string>(type: "TEXT", nullable: false),
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    Operation = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    LocalPath = table.Column<string>(type: "TEXT", nullable: false),
                    OneDriveId = table.Column<string>(type: "TEXT", nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    LocalHash = table.Column<string>(type: "TEXT", nullable: true),
                    RemoteHash = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedUtc_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_FileOperationLogs", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_FileOperationLogs_Accounts_HashedAccountId",
                        column: x => x.HashedAccountId,
                        principalTable: "Accounts",
                        principalColumn: "HashedAccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
                name: "SyncConflicts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    LocalModifiedUtc_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    RemoteModifiedUtc_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    LocalSize = table.Column<long>(type: "INTEGER", nullable: false),
                    RemoteSize = table.Column<long>(type: "INTEGER", nullable: false),
                    DetectedUtc_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    ResolutionStrategy = table.Column<int>(type: "INTEGER", nullable: false),
                    IsResolved = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_SyncConflicts", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_SyncConflicts_Accounts_HashedAccountId",
                        column: x => x.HashedAccountId,
                        principalTable: "Accounts",
                        principalColumn: "HashedAccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.CreateTable(
                name: "SyncSessionLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    HashedAccountId = table.Column<string>(type: "TEXT", nullable: false),
                    StartedUtc_Ticks = table.Column<long>(type: "INTEGER", nullable: false),
                    CompletedUtc_Ticks = table.Column<long>(type: "INTEGER", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FilesUploaded = table.Column<int>(type: "INTEGER", nullable: false),
                    FilesDownloaded = table.Column<int>(type: "INTEGER", nullable: false),
                    FilesDeleted = table.Column<int>(type: "INTEGER", nullable: false),
                    ConflictsDetected = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalBytes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    _ = table.PrimaryKey("PK_SyncSessionLogs", x => x.Id);
                    _ = table.ForeignKey(
                        name: "FK_SyncSessionLogs_Accounts_HashedAccountId",
                        column: x => x.HashedAccountId,
                        principalTable: "Accounts",
                        principalColumn: "HashedAccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            _ = migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "HashedAccountId", "AutoSyncIntervalMinutes", "DeltaToken", "DisplayName", "EnableDebugLogging", "EnableDetailedSyncLogging", "Id", "IsAuthenticated", "LastSyncUtc", "LocalSyncPath", "MaxItemsInBatch", "MaxParallelUpDownloads" },
                values: new object[] { "HashedAccountId { Id = C856527B9EAF27E26FD89183D1E4F2AEF3CEB5C8040D87A012A3F8F50DC55BB9 }", 0, null, "System Admin", true, true, "C856527B9EAF27E26FD89183D1E4F2AEF3CEB5C8040D87A012A3F8F50DC55BB9", true, null, ".", 1, 1 });

            _ = migrationBuilder.CreateIndex(
                name: "IX_Accounts_LocalSyncPath",
                table: "Accounts",
                column: "LocalSyncPath",
                unique: true);

            _ = migrationBuilder.CreateIndex(
                name: "IX_DebugLogs_HashedAccountId",
                table: "DebugLogs",
                column: "HashedAccountId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_DebugLogs_TimestampUtc_Ticks",
                table: "DebugLogs",
                column: "TimestampUtc_Ticks");

            _ = migrationBuilder.CreateIndex(
                name: "IX_DeltaTokens_HashedAccountId",
                table: "DeltaTokens",
                column: "HashedAccountId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_DriveItems_HashedAccountId_RelativePath",
                table: "DriveItems",
                columns: new[] { "HashedAccountId", "RelativePath" });

            _ = migrationBuilder.CreateIndex(
                name: "IX_DriveItems_IsFolder",
                table: "DriveItems",
                column: "IsFolder");

            _ = migrationBuilder.CreateIndex(
                name: "IX_DriveItems_IsSelected",
                table: "DriveItems",
                column: "IsSelected");

            _ = migrationBuilder.CreateIndex(
                name: "IX_FileOperationLogs_HashedAccountId",
                table: "FileOperationLogs",
                column: "HashedAccountId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_SyncConflicts_HashedAccountId",
                table: "SyncConflicts",
                column: "HashedAccountId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_SyncConflicts_HashedAccountId_IsResolved",
                table: "SyncConflicts",
                columns: new[] { "HashedAccountId", "IsResolved" });

            _ = migrationBuilder.CreateIndex(
                name: "IX_SyncSessionLogs_HashedAccountId",
                table: "SyncSessionLogs",
                column: "HashedAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropTable(
                name: "DebugLogs");

            _ = migrationBuilder.DropTable(
                name: "DeltaTokens");

            _ = migrationBuilder.DropTable(
                name: "DriveItems");

            _ = migrationBuilder.DropTable(
                name: "FileOperationLogs");

            _ = migrationBuilder.DropTable(
                name: "SyncConflicts");

            _ = migrationBuilder.DropTable(
                name: "SyncSessionLogs");

            _ = migrationBuilder.DropTable(
                name: "WindowPreferences");

            _ = migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
