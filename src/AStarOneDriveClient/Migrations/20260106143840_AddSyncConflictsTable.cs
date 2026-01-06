using System;
using Microsoft.EntityFrameworkCore.Migrations;

#pragma warning disable CA1861 // Prefer static readonly fields - EF Core generated migration code

#nullable disable

namespace AStarOneDriveClient.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncConflictsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncConflicts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    AccountId = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    LocalModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RemoteModifiedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LocalSize = table.Column<long>(type: "INTEGER", nullable: false),
                    RemoteSize = table.Column<long>(type: "INTEGER", nullable: false),
                    DetectedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
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
                name: "SyncConflicts");
        }
    }
}
