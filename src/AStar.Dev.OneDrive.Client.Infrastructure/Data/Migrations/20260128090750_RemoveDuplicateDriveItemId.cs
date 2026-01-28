using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AStar.Dev.OneDrive.Client.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicateDriveItemId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DriveItems_DriveItemId",
                table: "DriveItems");

            migrationBuilder.DropColumn(
                name: "DriveItemId",
                table: "DriveItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriveItemId",
                table: "DriveItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_DriveItems_DriveItemId",
                table: "DriveItems",
                column: "DriveItemId");
        }
    }
}
