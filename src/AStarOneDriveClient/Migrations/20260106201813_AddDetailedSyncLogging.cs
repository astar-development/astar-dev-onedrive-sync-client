using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStarOneDriveClient.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailedSyncLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EnableDetailedSyncLogging",
                table: "Accounts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "FileOperationLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    SyncSessionId = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Operation = table.Column<int>(type: "INTEGER", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    LocalPath = table.Column<string>(type: "TEXT", nullable: false),
                    OneDriveId = table.Column<string>(type: "TEXT", nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    LocalHash = table.Column<string>(type: "TEXT", nullable: true),
                    RemoteHash = table.Column<string>(type: "TEXT", nullable: true),
                    LastModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                    StartedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileOperationLogs");

            migrationBuilder.DropTable(
                name: "SyncSessionLogs");

            migrationBuilder.DropColumn(
                name: "EnableDetailedSyncLogging",
                table: "Accounts");
        }
    }
}
