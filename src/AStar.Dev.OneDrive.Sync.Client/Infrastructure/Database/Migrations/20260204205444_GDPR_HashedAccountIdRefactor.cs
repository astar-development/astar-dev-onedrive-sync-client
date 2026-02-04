using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class GDPR_HashedAccountIdRefactor : Migration
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
                name: "FK_DiagnosticSettings_Accounts_AccountId",
                schema: "onedrive",
                table: "DiagnosticSettings");

            migrationBuilder.DropForeignKey(
                name: "FK_FileSystemItems_Accounts_AccountId",
                schema: "onedrive",
                table: "FileSystemItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SyncHistory_Accounts_AccountId",
                schema: "onedrive",
                table: "SyncHistory");

            migrationBuilder.DropIndex(
                name: "IX_SyncHistory_AccountId",
                schema: "onedrive",
                table: "SyncHistory");

            migrationBuilder.DropIndex(
                name: "idx_applicationlogs_accountid_timestamp",
                schema: "onedrive",
                table: "ApplicationLogs");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                schema: "onedrive",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "IsActive",
                schema: "onedrive",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "LastAuthRefresh",
                schema: "onedrive",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "MaxConcurrentDownloads",
                schema: "onedrive",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "TokenStorageKey",
                schema: "onedrive",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                schema: "onedrive",
                table: "SyncHistory",
                newName: "HashedAccountId");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                schema: "onedrive",
                table: "DiagnosticSettings",
                newName: "HashedAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_DiagnosticSettings_AccountId",
                schema: "onedrive",
                table: "DiagnosticSettings",
                newName: "IX_DiagnosticSettings_HashedAccountId");

            migrationBuilder.RenameColumn(
                name: "MaxConcurrentUploads",
                schema: "onedrive",
                table: "Accounts",
                newName: "MaxConcurrent");

            migrationBuilder.RenameColumn(
                name: "EnableDebugLogging",
                schema: "onedrive",
                table: "Accounts",
                newName: "DebugLoggingEnabled");

            migrationBuilder.AlterColumn<Guid>(
                name: "AccountId",
                schema: "onedrive",
                table: "FileSystemItems",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "HashedAccountId",
                schema: "onedrive",
                table: "FileSystemItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "AccountId",
                schema: "onedrive",
                table: "DeltaTokens",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "HashedAccountId",
                schema: "onedrive",
                table: "DeltaTokens",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "AccountId",
                schema: "onedrive",
                table: "ConflictLogs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "HashedAccountId",
                schema: "onedrive",
                table: "ConflictLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "AccountId",
                schema: "onedrive",
                table: "ApplicationLogs",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HashedAccountId",
                schema: "onedrive",
                table: "ApplicationLogs",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsAdmin",
                schema: "onedrive",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "onedrive",
                table: "Accounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "onedrive",
                table: "Accounts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "HashedAccountId",
                schema: "onedrive",
                table: "Accounts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                schema: "onedrive",
                table: "Accounts",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_FileSystemItems_HashedAccountId",
                schema: "onedrive",
                table: "FileSystemItems",
                column: "HashedAccountId");

            migrationBuilder.CreateIndex(
                name: "idx_applicationlogs_hashedaccountid_timestamp",
                schema: "onedrive",
                table: "ApplicationLogs",
                columns: new[] { "HashedAccountId", "Timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLogs_AccountId",
                schema: "onedrive",
                table: "ApplicationLogs",
                column: "AccountId");

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

            migrationBuilder.DropIndex(
                name: "IX_FileSystemItems_HashedAccountId",
                schema: "onedrive",
                table: "FileSystemItems");

            migrationBuilder.DropIndex(
                name: "idx_applicationlogs_hashedaccountid_timestamp",
                schema: "onedrive",
                table: "ApplicationLogs");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationLogs_AccountId",
                schema: "onedrive",
                table: "ApplicationLogs");

            migrationBuilder.DropColumn(
                name: "HashedAccountId",
                schema: "onedrive",
                table: "FileSystemItems");

            migrationBuilder.DropColumn(
                name: "HashedAccountId",
                schema: "onedrive",
                table: "DeltaTokens");

            migrationBuilder.DropColumn(
                name: "HashedAccountId",
                schema: "onedrive",
                table: "ConflictLogs");

            migrationBuilder.DropColumn(
                name: "HashedAccountId",
                schema: "onedrive",
                table: "ApplicationLogs");

            migrationBuilder.DropColumn(
                name: "HashedAccountId",
                schema: "onedrive",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "onedrive",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "HashedAccountId",
                schema: "onedrive",
                table: "SyncHistory",
                newName: "AccountId");

            migrationBuilder.RenameColumn(
                name: "HashedAccountId",
                schema: "onedrive",
                table: "DiagnosticSettings",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_DiagnosticSettings_HashedAccountId",
                schema: "onedrive",
                table: "DiagnosticSettings",
                newName: "IX_DiagnosticSettings_AccountId");

            migrationBuilder.RenameColumn(
                name: "MaxConcurrent",
                schema: "onedrive",
                table: "Accounts",
                newName: "MaxConcurrentUploads");

            migrationBuilder.RenameColumn(
                name: "DebugLoggingEnabled",
                schema: "onedrive",
                table: "Accounts",
                newName: "EnableDebugLogging");

            migrationBuilder.AlterColumn<string>(
                name: "AccountId",
                schema: "onedrive",
                table: "FileSystemItems",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AccountId",
                schema: "onedrive",
                table: "DeltaTokens",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AccountId",
                schema: "onedrive",
                table: "ConflictLogs",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AccountId",
                schema: "onedrive",
                table: "ApplicationLogs",
                type: "text",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsAdmin",
                schema: "onedrive",
                table: "Accounts",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                schema: "onedrive",
                table: "Accounts",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                schema: "onedrive",
                table: "Accounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                schema: "onedrive",
                table: "Accounts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                schema: "onedrive",
                table: "Accounts",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAuthRefresh",
                schema: "onedrive",
                table: "Accounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxConcurrentDownloads",
                schema: "onedrive",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 5);

            migrationBuilder.AddColumn<string>(
                name: "TokenStorageKey",
                schema: "onedrive",
                table: "Accounts",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncHistory_AccountId",
                schema: "onedrive",
                table: "SyncHistory",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "idx_applicationlogs_accountid_timestamp",
                schema: "onedrive",
                table: "ApplicationLogs",
                columns: new[] { "AccountId", "Timestamp" },
                descending: new[] { false, true });

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
                name: "FK_DiagnosticSettings_Accounts_AccountId",
                schema: "onedrive",
                table: "DiagnosticSettings",
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

            migrationBuilder.AddForeignKey(
                name: "FK_SyncHistory_Accounts_AccountId",
                schema: "onedrive",
                table: "SyncHistory",
                column: "AccountId",
                principalSchema: "onedrive",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
