using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDebLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.CreateIndex(
                name: "IX_DebugLogs_AccountId",
                table: "DebugLogs",
                column: "AccountId");

            _ = migrationBuilder.CreateIndex(
                name: "IX_DebugLogs_TimestampUtc",
                table: "DebugLogs",
                column: "TimestampUtc");

            _ = migrationBuilder.AddForeignKey(
                name: "FK_DebugLogs_Accounts_AccountId",
                table: "DebugLogs",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "AccountId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropForeignKey(
                name: "FK_DebugLogs_Accounts_AccountId",
                table: "DebugLogs");

            _ = migrationBuilder.DropIndex(
                name: "IX_DebugLogs_AccountId",
                table: "DebugLogs");

            _ = migrationBuilder.DropIndex(
                name: "IX_DebugLogs_TimestampUtc",
                table: "DebugLogs");
        }
    }
}
