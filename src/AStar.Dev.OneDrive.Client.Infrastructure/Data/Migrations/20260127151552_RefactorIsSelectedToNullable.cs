using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class RefactorIsSelectedToNullable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AlterColumn<bool>(
            name: "IsSelected",
            table: "DriveItems",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(bool),
            oldType: "INTEGER");

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.AlterColumn<bool>(
            name: "IsSelected",
            table: "DriveItems",
            type: "INTEGER",
            nullable: false,
            defaultValue: false,
            oldClrType: typeof(bool),
            oldType: "INTEGER",
            oldNullable: true);
}
