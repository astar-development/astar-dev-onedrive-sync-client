using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class UpdatePropertyTypes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropColumn(
            name: "Status",
            schema: "onedrive",
            table: "SyncHistory");

        _ = migrationBuilder.AlterColumn<int>(
            name: "SyncType",
            schema: "onedrive",
            table: "SyncHistory",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        _ = migrationBuilder.AlterColumn<int>(
            name: "SyncDirection",
            schema: "onedrive",
            table: "SyncHistory",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        _ = migrationBuilder.AddColumn<int>(
            name: "SyncResult",
            schema: "onedrive",
            table: "SyncHistory",
            type: "integer",
            nullable: false,
            defaultValue: 0);

        _ = migrationBuilder.AlterColumn<int>(
            name: "SyncStatus",
            schema: "onedrive",
            table: "FileSystemItems",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        _ = migrationBuilder.AlterColumn<int>(
            name: "LastSyncDirection",
            schema: "onedrive",
            table: "FileSystemItems",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        _ = migrationBuilder.AlterColumn<int>(
            name: "LogLevel",
            schema: "onedrive",
            table: "DiagnosticSettings",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        _ = migrationBuilder.AlterColumn<int>(
            name: "ResolutionAction",
            schema: "onedrive",
            table: "ConflictLogs",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        _ = migrationBuilder.AlterColumn<int>(
            name: "ConflictType",
            schema: "onedrive",
            table: "ConflictLogs",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(string),
            oldType: "text",
            oldNullable: true);

        _ = migrationBuilder.AlterColumn<int>(
            name: "LogLevel",
            schema: "onedrive",
            table: "ApplicationLogs",
            type: "integer",
            nullable: false,
            oldClrType: typeof(string),
            oldType: "text");

        _ = migrationBuilder.AddColumn<bool>(
            name: "IsAdmin",
            schema: "onedrive",
            table: "Accounts",
            type: "boolean",
            nullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.DropColumn(
            name: "SyncResult",
            schema: "onedrive",
            table: "SyncHistory");

        _ = migrationBuilder.DropColumn(
            name: "IsAdmin",
            schema: "onedrive",
            table: "Accounts");

        _ = migrationBuilder.AlterColumn<string>(
            name: "SyncType",
            schema: "onedrive",
            table: "SyncHistory",
            type: "text",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer");

        _ = migrationBuilder.AlterColumn<string>(
            name: "SyncDirection",
            schema: "onedrive",
            table: "SyncHistory",
            type: "text",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer");

        _ = migrationBuilder.AddColumn<string>(
            name: "Status",
            schema: "onedrive",
            table: "SyncHistory",
            type: "text",
            nullable: true);

        _ = migrationBuilder.AlterColumn<string>(
            name: "SyncStatus",
            schema: "onedrive",
            table: "FileSystemItems",
            type: "text",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer");

        _ = migrationBuilder.AlterColumn<string>(
            name: "LastSyncDirection",
            schema: "onedrive",
            table: "FileSystemItems",
            type: "text",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer");

        _ = migrationBuilder.AlterColumn<string>(
            name: "LogLevel",
            schema: "onedrive",
            table: "DiagnosticSettings",
            type: "text",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer");

        _ = migrationBuilder.AlterColumn<string>(
            name: "ResolutionAction",
            schema: "onedrive",
            table: "ConflictLogs",
            type: "text",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer");

        _ = migrationBuilder.AlterColumn<string>(
            name: "ConflictType",
            schema: "onedrive",
            table: "ConflictLogs",
            type: "text",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "integer");

        _ = migrationBuilder.AlterColumn<string>(
            name: "LogLevel",
            schema: "onedrive",
            table: "ApplicationLogs",
            type: "text",
            nullable: false,
            oldClrType: typeof(int),
            oldType: "integer");
    }
}
