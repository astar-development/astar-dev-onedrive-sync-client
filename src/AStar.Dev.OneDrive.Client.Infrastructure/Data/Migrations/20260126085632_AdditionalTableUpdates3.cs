using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdditionalTableUpdates3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropIndex(
                name: "IX_DriveItems_AccountId",
                table: "DriveItems");

            _ = migrationBuilder.AddColumn<bool>(
                name: "IsSelected",
                table: "DriveItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            _ = migrationBuilder.CreateIndex(
                name: "IX_DriveItems_AccountId_RelativePath",
                table: "DriveItems",
                columns: new[] { "AccountId", "RelativePath" });

            _ = migrationBuilder.CreateIndex(
                name: "IX_DriveItems_IsFolder",
                table: "DriveItems",
                column: "IsFolder");

            _ = migrationBuilder.CreateIndex(
                name: "IX_DriveItems_IsSelected",
                table: "DriveItems",
                column: "IsSelected");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            _ = migrationBuilder.DropIndex(
                name: "IX_DriveItems_AccountId_RelativePath",
                table: "DriveItems");

            _ = migrationBuilder.DropIndex(
                name: "IX_DriveItems_IsFolder",
                table: "DriveItems");

            _ = migrationBuilder.DropIndex(
                name: "IX_DriveItems_IsSelected",
                table: "DriveItems");

            _ = migrationBuilder.DropColumn(
                name: "IsSelected",
                table: "DriveItems");

            _ = migrationBuilder.CreateIndex(
                name: "IX_DriveItems_AccountId",
                table: "DriveItems",
                column: "AccountId");
        }
    }
}
