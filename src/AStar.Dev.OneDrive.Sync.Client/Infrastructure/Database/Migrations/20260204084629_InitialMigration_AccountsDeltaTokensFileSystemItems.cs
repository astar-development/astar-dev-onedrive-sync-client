using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class InitialMigration_AccountsDeltaTokensFileSystemItems : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.EnsureSchema(
            name: "onedrive");

        _ = migrationBuilder.CreateTable(
            name: "Accounts",
            schema: "onedrive",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                HashedEmail = table.Column<string>(type: "text", nullable: false),
                DisplayName = table.Column<string>(type: "text", nullable: true),
                TokenStorageKey = table.Column<string>(type: "text", nullable: true),
                HomeSyncDirectory = table.Column<string>(type: "text", nullable: true),
                MaxConcurrentDownloads = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                MaxConcurrentUploads = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                EnableDebugLogging = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LastAuthRefresh = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: true)
            },
            constraints: table => _ = table.PrimaryKey("PK_Accounts", x => x.Id));

        _ = migrationBuilder.CreateTable(
            name: "DeltaTokens",
            schema: "onedrive",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                AccountId = table.Column<string>(type: "text", nullable: false),
                DriveName = table.Column<string>(type: "text", nullable: false),
                Token = table.Column<string>(type: "text", nullable: true),
                LastSyncAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_DeltaTokens", x => x.Id);
                _ = table.ForeignKey(
                    name: "FK_DeltaTokens_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalSchema: "onedrive",
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        _ = migrationBuilder.CreateTable(
            name: "FileSystemItems",
            schema: "onedrive",
            columns: table => new
            {
                Id = table.Column<string>(type: "text", nullable: false),
                AccountId = table.Column<string>(type: "text", nullable: false),
                DriveItemId = table.Column<string>(type: "text", nullable: false),
                Name = table.Column<string>(type: "text", nullable: false),
                Path = table.Column<string>(type: "text", nullable: false),
                IsFolder = table.Column<bool>(type: "boolean", nullable: false),
                ParentItemId = table.Column<string>(type: "text", nullable: true),
                IsSelected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                LocalPath = table.Column<string>(type: "text", nullable: true),
                RemoteModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                LocalModifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                RemoteHash = table.Column<string>(type: "text", nullable: true),
                LocalHash = table.Column<string>(type: "text", nullable: true),
                SyncStatus = table.Column<string>(type: "text", nullable: true),
                LastSyncDirection = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_FileSystemItems", x => x.Id);
                _ = table.ForeignKey(
                    name: "FK_FileSystemItems_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalSchema: "onedrive",
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        _ = migrationBuilder.CreateIndex(
            name: "IX_Accounts_HashedEmail",
            schema: "onedrive",
            table: "Accounts",
            column: "HashedEmail",
            unique: true);

        _ = migrationBuilder.CreateIndex(
            name: "IX_DeltaTokens_AccountId",
            schema: "onedrive",
            table: "DeltaTokens",
            column: "AccountId");

        _ = migrationBuilder.CreateIndex(
            name: "IX_FileSystemItems_AccountId",
            schema: "onedrive",
            table: "FileSystemItems",
            column: "AccountId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "DeltaTokens",
            schema: "onedrive");

        _ = migrationBuilder.DropTable(
            name: "FileSystemItems",
            schema: "onedrive");

        _ = migrationBuilder.DropTable(
            name: "Accounts",
            schema: "onedrive");
    }
}
