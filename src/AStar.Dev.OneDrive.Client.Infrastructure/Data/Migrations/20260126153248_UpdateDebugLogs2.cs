using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDebugLogs2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.RenameColumn(
                name: "LastModifiedUtc",
                table: "DriveItems",
                newName: "LastModifiedUtc_Ticks");

            _ = migrationBuilder.RenameColumn(
                name: "TimestampUtc",
                table: "DebugLogs",
                newName: "TimestampUtc_Ticks");

            _ = migrationBuilder.RenameIndex(
                name: "IX_DebugLogs_TimestampUtc",
                table: "DebugLogs",
                newName: "IX_DebugLogs_TimestampUtc_Ticks");

            _ = migrationBuilder.AlterColumn<long>(
                name: "LastModifiedUtc_Ticks",
                table: "DriveItems",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            _ = migrationBuilder.AlterColumn<long>(
                name: "TimestampUtc_Ticks",
                table: "DebugLogs",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.RenameColumn(
                name: "LastModifiedUtc_Ticks",
                table: "DriveItems",
                newName: "LastModifiedUtc");

            _ = migrationBuilder.RenameColumn(
                name: "TimestampUtc_Ticks",
                table: "DebugLogs",
                newName: "TimestampUtc");

            _ = migrationBuilder.RenameIndex(
                name: "IX_DebugLogs_TimestampUtc_Ticks",
                table: "DebugLogs",
                newName: "IX_DebugLogs_TimestampUtc");

            _ = migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastModifiedUtc",
                table: "DriveItems",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            _ = migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "TimestampUtc",
                table: "DebugLogs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");
        }
    }
}
