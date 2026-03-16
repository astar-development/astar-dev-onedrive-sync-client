using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    AccentIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DeltaLink = table.Column<string>(type: "TEXT", nullable: true),
                    LastSyncedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    QuotaTotal = table.Column<long>(type: "INTEGER", nullable: false),
                    QuotaUsed = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncFolders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FolderId = table.Column<string>(type: "TEXT", nullable: false),
                    FolderName = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    DeltaLink = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncFolders_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncFolders_AccountId_FolderId",
                table: "SyncFolders",
                columns: new[] { "AccountId", "FolderId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncFolders");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
