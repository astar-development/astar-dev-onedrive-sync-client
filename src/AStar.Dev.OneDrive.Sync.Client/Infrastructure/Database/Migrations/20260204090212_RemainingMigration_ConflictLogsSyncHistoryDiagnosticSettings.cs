using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RemainingMigration_ConflictLogsSyncHistoryDiagnosticSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConflictLogs",
                schema: "onedrive",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<string>(type: "text", nullable: false),
                    ItemId = table.Column<string>(type: "text", nullable: false),
                    LocalPath = table.Column<string>(type: "text", nullable: true),
                    ConflictType = table.Column<string>(type: "text", nullable: true),
                    LocalLastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RemoteLastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResolutionAction = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConflictLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConflictLogs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "onedrive",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DiagnosticSettings",
                schema: "onedrive",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<string>(type: "text", nullable: false),
                    LogLevel = table.Column<string>(type: "text", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiagnosticSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiagnosticSettings_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "onedrive",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncHistory",
                schema: "onedrive",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AccountId = table.Column<string>(type: "text", nullable: false),
                    SyncType = table.Column<string>(type: "text", nullable: true),
                    SyncDirection = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    ItemsUploaded = table.Column<int>(type: "integer", nullable: true),
                    ItemsDownloaded = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncHistory_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "onedrive",
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConflictLogs_AccountId",
                schema: "onedrive",
                table: "ConflictLogs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DiagnosticSettings_AccountId",
                schema: "onedrive",
                table: "DiagnosticSettings",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncHistory_AccountId",
                schema: "onedrive",
                table: "SyncHistory",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConflictLogs",
                schema: "onedrive");

            migrationBuilder.DropTable(
                name: "DiagnosticSettings",
                schema: "onedrive");

            migrationBuilder.DropTable(
                name: "SyncHistory",
                schema: "onedrive");
        }
    }
}
