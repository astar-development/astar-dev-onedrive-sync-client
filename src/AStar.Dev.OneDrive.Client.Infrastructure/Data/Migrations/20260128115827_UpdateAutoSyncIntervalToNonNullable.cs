using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class UpdateAutoSyncIntervalToNonNullable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.RenameColumn(
            name: "Id",
            table: "DriveItems",
            newName: "DriveItemId");

        _ = migrationBuilder.AlterColumn<int>(
            name: "AutoSyncIntervalMinutes",
            table: "Accounts",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0,
            oldClrType: typeof(int),
            oldType: "INTEGER",
            oldNullable: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.RenameColumn(
            name: "DriveItemId",
            table: "DriveItems",
            newName: "Id");

        _ = migrationBuilder.AlterColumn<int>(
            name: "AutoSyncIntervalMinutes",
            table: "Accounts",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(int),
            oldType: "INTEGER");
    }
}
