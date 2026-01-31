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
            keyValue: "e29a2798-c836-4854-ac90-a3f2d37aae26",
            column: "LastSyncUtc",
            value: null);

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) => _ = migrationBuilder.UpdateData(
            table: "Accounts",
            keyColumn: "AccountId",
            keyValue: "e29a2798-c836-4854-ac90-a3f2d37aae26",
            column: "LastSyncUtc",
            value: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
}
