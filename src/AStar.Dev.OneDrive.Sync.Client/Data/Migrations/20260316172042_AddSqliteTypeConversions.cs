using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSqliteTypeConversions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RemoteModified",
                table: "SyncJobs",
                newName: "RemoteModified_Ticks");

            migrationBuilder.RenameColumn(
                name: "QueuedAt",
                table: "SyncJobs",
                newName: "QueuedAt_Ticks");

            migrationBuilder.RenameColumn(
                name: "CompletedAt",
                table: "SyncJobs",
                newName: "CompletedAt_Ticks");

            migrationBuilder.RenameColumn(
                name: "ResolvedAt",
                table: "SyncConflicts",
                newName: "ResolvedAt_Ticks");

            migrationBuilder.RenameColumn(
                name: "RemoteModified",
                table: "SyncConflicts",
                newName: "RemoteModified_Ticks");

            migrationBuilder.RenameColumn(
                name: "LocalModified",
                table: "SyncConflicts",
                newName: "LocalModified_Ticks");

            migrationBuilder.RenameColumn(
                name: "DetectedAt",
                table: "SyncConflicts",
                newName: "DetectedAt_Ticks");

            migrationBuilder.RenameColumn(
                name: "LastSyncedAt",
                table: "Accounts",
                newName: "LastSyncedAt_Ticks");

            migrationBuilder.AlterColumn<byte[]>(
                name: "Id",
                table: "SyncJobs",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "RemoteModified_Ticks",
                table: "SyncJobs",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "QueuedAt_Ticks",
                table: "SyncJobs",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "CompletedAt_Ticks",
                table: "SyncJobs",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "Id",
                table: "SyncConflicts",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "ResolvedAt_Ticks",
                table: "SyncConflicts",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "RemoteModified_Ticks",
                table: "SyncConflicts",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "LocalModified_Ticks",
                table: "SyncConflicts",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "DetectedAt_Ticks",
                table: "SyncConflicts",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<long>(
                name: "LastSyncedAt_Ticks",
                table: "Accounts",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RemoteModified_Ticks",
                table: "SyncJobs",
                newName: "RemoteModified");

            migrationBuilder.RenameColumn(
                name: "QueuedAt_Ticks",
                table: "SyncJobs",
                newName: "QueuedAt");

            migrationBuilder.RenameColumn(
                name: "CompletedAt_Ticks",
                table: "SyncJobs",
                newName: "CompletedAt");

            migrationBuilder.RenameColumn(
                name: "ResolvedAt_Ticks",
                table: "SyncConflicts",
                newName: "ResolvedAt");

            migrationBuilder.RenameColumn(
                name: "RemoteModified_Ticks",
                table: "SyncConflicts",
                newName: "RemoteModified");

            migrationBuilder.RenameColumn(
                name: "LocalModified_Ticks",
                table: "SyncConflicts",
                newName: "LocalModified");

            migrationBuilder.RenameColumn(
                name: "DetectedAt_Ticks",
                table: "SyncConflicts",
                newName: "DetectedAt");

            migrationBuilder.RenameColumn(
                name: "LastSyncedAt_Ticks",
                table: "Accounts",
                newName: "LastSyncedAt");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SyncJobs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "RemoteModified",
                table: "SyncJobs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "QueuedAt",
                table: "SyncJobs",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "SyncJobs",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                table: "SyncConflicts",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "BLOB");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ResolvedAt",
                table: "SyncConflicts",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "RemoteModified",
                table: "SyncConflicts",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LocalModified",
                table: "SyncConflicts",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "DetectedAt",
                table: "SyncConflicts",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastSyncedAt",
                table: "Accounts",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
