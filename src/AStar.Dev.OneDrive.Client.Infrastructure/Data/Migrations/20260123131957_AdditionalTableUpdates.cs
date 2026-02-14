using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class AdditionalTableUpdates : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder) => _ = migrationBuilder.UpdateData(
            table: "Accounts",
            keyColumn: "AccountId",
            keyValue: "C856527B9EAF27E26FD89183D1E4F2AEF3CEB5C8040D87A012A3F8F50DC55BB9",
            column: "LastSyncUtc",
            value: null);

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) => _ = migrationBuilder.UpdateData(
            table: "Accounts",
            keyColumn: "AccountId",
            keyValue: "C856527B9EAF27E26FD89183D1E4F2AEF3CEB5C8040D87A012A3F8F50DC55BB9",
            column: "LastSyncUtc",
            value: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
}
