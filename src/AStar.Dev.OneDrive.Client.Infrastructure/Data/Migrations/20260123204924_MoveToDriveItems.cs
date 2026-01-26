using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class MoveToDriveItems : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropTable(
            name: "FileMetadata");

        _ = migrationBuilder.AddColumn<int>(
            name: "LastSyncDirection",
            table: "DriveItems",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);

        _ = migrationBuilder.AddColumn<string>(
            name: "LocalHash",
            table: "DriveItems",
            type: "TEXT",
            nullable: true);

        _ = migrationBuilder.AddColumn<string>(
            name: "LocalPath",
            table: "DriveItems",
            type: "TEXT",
            nullable: true);

        _ = migrationBuilder.AddColumn<string>(
            name: "Name",
            table: "DriveItems",
            type: "TEXT",
            nullable: true);

        _ = migrationBuilder.AddColumn<int>(
            name: "SyncStatus",
            table: "DriveItems",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropColumn(
            name: "LastSyncDirection",
            table: "DriveItems");

        _ = migrationBuilder.DropColumn(
            name: "LocalHash",
            table: "DriveItems");

        _ = migrationBuilder.DropColumn(
            name: "LocalPath",
            table: "DriveItems");

        _ = migrationBuilder.DropColumn(
            name: "Name",
            table: "DriveItems");

        _ = migrationBuilder.DropColumn(
            name: "SyncStatus",
            table: "DriveItems");

        _ = migrationBuilder.CreateTable(
            name: "FileMetadata",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                AccountId = table.Column<string>(type: "TEXT", nullable: false),
                CTag = table.Column<string>(type: "TEXT", nullable: true),
                ETag = table.Column<string>(type: "TEXT", nullable: true),
                LastModifiedUtc = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                LastSyncDirection = table.Column<int>(type: "INTEGER", nullable: true),
                LocalHash = table.Column<string>(type: "TEXT", nullable: true),
                LocalPath = table.Column<string>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", nullable: false),
                Path = table.Column<string>(type: "TEXT", nullable: false),
                Size = table.Column<long>(type: "INTEGER", nullable: false),
                SyncStatus = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_FileMetadata", x => x.Id);
                _ = table.ForeignKey(
                    name: "FK_FileMetadata_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalTable: "Accounts",
                    principalColumn: "AccountId",
                    onDelete: ReferentialAction.Cascade);
            });

        _ = migrationBuilder.CreateIndex(
            name: "IX_FileMetadata_AccountId",
            table: "FileMetadata",
            column: "AccountId");

        _ = migrationBuilder.CreateIndex(
            name: "IX_FileMetadata_AccountId_Path",
            table: "FileMetadata",
            columns: new[] { "AccountId", "Path" });
    }
}
