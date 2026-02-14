using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAccountIdToHashedId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "AccountId",
                keyValue: "C856527B9EAF27E26FD89183D1E4F2AEF3CEB5C8040D87A012A3F8F50DC55BB9");

            _ = migrationBuilder.InsertData(
                table: "Accounts",
                columns: ["AccountId", "AutoSyncIntervalMinutes", "DeltaToken", "DisplayName", "EnableDebugLogging", "EnableDetailedSyncLogging", "IsAuthenticated", "LastSyncUtc", "LocalSyncPath", "MaxItemsInBatch", "MaxParallelUpDownloads"],
                values: ["C856527B9EAF27E26FD89183D1E4F2AEF3CEB5C8040D87A012A3F8F50DC55BB9", 0, null, "System Admin", true, true, true, null, ".", 1, 1]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "AccountId",
                keyValue: "C856527B9EAF27E26FD89183D1E4F2AEF3CEB5C8040D87A012A3F8F50DC55BB9");

            _ = migrationBuilder.InsertData(
                table: "Accounts",
                columns: ["AccountId", "AutoSyncIntervalMinutes", "DeltaToken", "DisplayName", "EnableDebugLogging", "EnableDetailedSyncLogging", "IsAuthenticated", "LastSyncUtc", "LocalSyncPath", "MaxItemsInBatch", "MaxParallelUpDownloads"],
                values: ["C856527B9EAF27E26FD89183D1E4F2AEF3CEB5C8040D87A012A3F8F50DC55BB9", 0, null, "System Admin", true, true, true, null, ".", 1, 1]);
        }
    }
}
