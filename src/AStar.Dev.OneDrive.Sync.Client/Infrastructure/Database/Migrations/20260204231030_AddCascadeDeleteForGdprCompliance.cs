using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeDeleteForGdprCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationLogs_Accounts_AccountId",
                schema: "onedrive",
                table: "ApplicationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ConflictLogs_Accounts_AccountId",
                schema: "onedrive",
                table: "ConflictLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DeltaTokens_Accounts_AccountId",
                schema: "onedrive",
                table: "DeltaTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_FileSystemItems_Accounts_AccountId",
                schema: "onedrive",
                table: "FileSystemItems");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationLogs_Accounts_AccountId",
                schema: "onedrive",
                table: "ApplicationLogs",
                column: "AccountId",
                principalSchema: "onedrive",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConflictLogs_Accounts_AccountId",
                schema: "onedrive",
                table: "ConflictLogs",
                column: "AccountId",
                principalSchema: "onedrive",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DeltaTokens_Accounts_AccountId",
                schema: "onedrive",
                table: "DeltaTokens",
                column: "AccountId",
                principalSchema: "onedrive",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FileSystemItems_Accounts_AccountId",
                schema: "onedrive",
                table: "FileSystemItems",
                column: "AccountId",
                principalSchema: "onedrive",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationLogs_Accounts_AccountId",
                schema: "onedrive",
                table: "ApplicationLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_ConflictLogs_Accounts_AccountId",
                schema: "onedrive",
                table: "ConflictLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_DeltaTokens_Accounts_AccountId",
                schema: "onedrive",
                table: "DeltaTokens");

            migrationBuilder.DropForeignKey(
                name: "FK_FileSystemItems_Accounts_AccountId",
                schema: "onedrive",
                table: "FileSystemItems");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationLogs_Accounts_AccountId",
                schema: "onedrive",
                table: "ApplicationLogs",
                column: "AccountId",
                principalSchema: "onedrive",
                principalTable: "Accounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ConflictLogs_Accounts_AccountId",
                schema: "onedrive",
                table: "ConflictLogs",
                column: "AccountId",
                principalSchema: "onedrive",
                principalTable: "Accounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DeltaTokens_Accounts_AccountId",
                schema: "onedrive",
                table: "DeltaTokens",
                column: "AccountId",
                principalSchema: "onedrive",
                principalTable: "Accounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FileSystemItems_Accounts_AccountId",
                schema: "onedrive",
                table: "FileSystemItems",
                column: "AccountId",
                principalSchema: "onedrive",
                principalTable: "Accounts",
                principalColumn: "Id");
        }
    }
}
