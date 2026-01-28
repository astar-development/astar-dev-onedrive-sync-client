using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class RefactorFileOperationLogs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.RenameColumn(
            name: "StartedUtc",
            table: "SyncSessionLogs",
            newName: "StartedUtc_Ticks");

        _ = migrationBuilder.RenameColumn(
            name: "CompletedUtc",
            table: "SyncSessionLogs",
            newName: "CompletedUtc_Ticks");

        _ = migrationBuilder.RenameColumn(
            name: "Timestamp",
            table: "FileOperationLogs",
            newName: "Timestamp_Ticks");

        _ = migrationBuilder.RenameColumn(
            name: "LastModifiedUtc",
            table: "FileOperationLogs",
            newName: "LastModifiedUtc_Ticks");

        _ = migrationBuilder.AlterColumn<long>(
            name: "StartedUtc_Ticks",
            table: "SyncSessionLogs",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "TEXT");

        _ = migrationBuilder.AlterColumn<long>(
            name: "CompletedUtc_Ticks",
            table: "SyncSessionLogs",
            type: "INTEGER",
            nullable: true,
            oldClrType: typeof(DateTimeOffset),
            oldType: "TEXT",
            oldNullable: true);

        _ = migrationBuilder.AlterColumn<long>(
            name: "Timestamp_Ticks",
            table: "FileOperationLogs",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "TEXT");

        _ = migrationBuilder.AlterColumn<long>(
            name: "LastModifiedUtc_Ticks",
            table: "FileOperationLogs",
            type: "INTEGER",
            nullable: false,
            oldClrType: typeof(DateTimeOffset),
            oldType: "TEXT");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.RenameColumn(
            name: "StartedUtc_Ticks",
            table: "SyncSessionLogs",
            newName: "StartedUtc");

        _ = migrationBuilder.RenameColumn(
            name: "CompletedUtc_Ticks",
            table: "SyncSessionLogs",
            newName: "CompletedUtc");

        _ = migrationBuilder.RenameColumn(
            name: "Timestamp_Ticks",
            table: "FileOperationLogs",
            newName: "Timestamp");

        _ = migrationBuilder.RenameColumn(
            name: "LastModifiedUtc_Ticks",
            table: "FileOperationLogs",
            newName: "LastModifiedUtc");

        _ = migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "StartedUtc",
            table: "SyncSessionLogs",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");

        _ = migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "CompletedUtc",
            table: "SyncSessionLogs",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(long),
            oldType: "INTEGER",
            oldNullable: true);

        _ = migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "Timestamp",
            table: "FileOperationLogs",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");

        _ = migrationBuilder.AlterColumn<DateTimeOffset>(
            name: "LastModifiedUtc",
            table: "FileOperationLogs",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(long),
            oldType: "INTEGER");
    }
}
