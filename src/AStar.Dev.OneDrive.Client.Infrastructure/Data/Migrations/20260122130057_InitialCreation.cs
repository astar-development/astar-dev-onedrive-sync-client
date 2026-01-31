using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    LocalSyncPath = table.Column<string>(type: "TEXT", nullable: false),
                    IsAuthenticated = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastSyncUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    DeltaToken = table.Column<string>(type: "TEXT", nullable: true),
                    EnableDetailedSyncLogging = table.Column<bool>(type: "INTEGER", nullable: false),
                    EnableDebugLogging = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaxParallelUpDownloads = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxItemsInBatch = table.Column<int>(type: "INTEGER", nullable: false),
                    AutoSyncIntervalMinutes = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "DebugLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LogLevel = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Exception = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DebugLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FileOperationLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SyncSessionId = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Operation = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    LocalPath = table.Column<string>(type: "TEXT", nullable: false),
                    OneDriveId = table.Column<string>(type: "TEXT", nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    LocalHash = table.Column<string>(type: "TEXT", nullable: true),
                    RemoteHash = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileOperationLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncSessionLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    StartedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    FilesUploaded = table.Column<int>(type: "INTEGER", nullable: false),
                    FilesDownloaded = table.Column<int>(type: "INTEGER", nullable: false),
                    FilesDeleted = table.Column<int>(type: "INTEGER", nullable: false),
                    ConflictsDetected = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalBytes = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncSessionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WindowPreferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    X = table.Column<double>(type: "REAL", nullable: true),
                    Y = table.Column<double>(type: "REAL", nullable: true),
                    Width = table.Column<double>(type: "REAL", nullable: false, defaultValue: 800.0),
                    Height = table.Column<double>(type: "REAL", nullable: false, defaultValue: 600.0),
                    IsMaximized = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WindowPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeltaTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: false),
                    LastSyncedUtc_Ticks = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeltaTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeltaTokens_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DriveItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    DriveItemId = table.Column<string>(type: "TEXT", nullable: false),
                    RelativePath = table.Column<string>(type: "TEXT", nullable: false),
                    ETag = table.Column<string>(type: "TEXT", nullable: true),
                    CTag = table.Column<string>(type: "TEXT", nullable: true),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsFolder = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriveItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriveItems_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FileMetadata",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    LocalPath = table.Column<string>(type: "TEXT", nullable: false),
                    CTag = table.Column<string>(type: "TEXT", nullable: true),
                    ETag = table.Column<string>(type: "TEXT", nullable: true),
                    LocalHash = table.Column<string>(type: "TEXT", nullable: true),
                    SyncStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    LastSyncDirection = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileMetadata", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileMetadata_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    FolderPath = table.Column<string>(type: "TEXT", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncConfigurations_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncConflicts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
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
                    table.PrimaryKey("PK_SyncConflicts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncConflicts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "AccountId", "AutoSyncIntervalMinutes", "DeltaToken", "DisplayName", "EnableDebugLogging", "EnableDetailedSyncLogging", "IsAuthenticated", "LastSyncUtc", "LocalSyncPath", "MaxItemsInBatch", "MaxParallelUpDownloads" },
                values: new object[] { "e29a2798-c836-4854-ac90-a3f2d37aae26", 0, null, "System Admin", true, true, true, new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), ".", 1, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_LocalSyncPath",
                table: "Accounts",
                column: "LocalSyncPath",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeltaTokens_AccountId",
                table: "DeltaTokens",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DriveItems_AccountId",
                table: "DriveItems",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DriveItems_DriveItemId",
                table: "DriveItems",
                column: "DriveItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_AccountId",
                table: "FileMetadata",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FileMetadata_AccountId_Path",
                table: "FileMetadata",
                columns: new[] { "AccountId", "Path" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncConfigurations_AccountId_FolderPath",
                table: "SyncConfigurations",
                columns: new[] { "AccountId", "FolderPath" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflicts_AccountId",
                table: "SyncConflicts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncConflicts_AccountId_IsResolved",
                table: "SyncConflicts",
                columns: new[] { "AccountId", "IsResolved" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DebugLogs");

            migrationBuilder.DropTable(
                name: "DeltaTokens");

            migrationBuilder.DropTable(
                name: "DriveItems");

            migrationBuilder.DropTable(
                name: "FileMetadata");

            migrationBuilder.DropTable(
                name: "FileOperationLogs");

            migrationBuilder.DropTable(
                name: "SyncConfigurations");

            migrationBuilder.DropTable(
                name: "SyncConflicts");

            migrationBuilder.DropTable(
                name: "SyncSessionLogs");

            migrationBuilder.DropTable(
                name: "WindowPreferences");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
