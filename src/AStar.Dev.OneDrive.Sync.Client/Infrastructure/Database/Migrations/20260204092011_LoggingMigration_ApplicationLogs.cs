using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AStar.Dev.OneDrive.Sync.Client.Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class LoggingMigration_ApplicationLogs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        _ = migrationBuilder.CreateTable(
            name: "ApplicationLogs",
            schema: "onedrive",
            columns: table => new
            {
                Id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                AccountId = table.Column<string>(type: "text", nullable: true),
                LogLevel = table.Column<string>(type: "text", nullable: false),
                Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                Message = table.Column<string>(type: "text", nullable: true),
                Exception = table.Column<string>(type: "text", nullable: true),
                SourceContext = table.Column<string>(type: "text", nullable: true),
                Properties = table.Column<string>(type: "jsonb", nullable: true)
            },
            constraints: table =>
            {
                _ = table.PrimaryKey("PK_ApplicationLogs", x => x.Id);
                _ = table.ForeignKey(
                    name: "FK_ApplicationLogs_Accounts_AccountId",
                    column: x => x.AccountId,
                    principalSchema: "onedrive",
                    principalTable: "Accounts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        _ = migrationBuilder.CreateIndex(
            name: "idx_applicationlogs_accountid_timestamp",
            schema: "onedrive",
            table: "ApplicationLogs",
            columns: new[] { "AccountId", "Timestamp" },
            descending: new[] { false, true });

        _ = migrationBuilder.CreateIndex(
            name: "idx_applicationlogs_loglevel",
            schema: "onedrive",
            table: "ApplicationLogs",
            column: "LogLevel");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder) => _ = migrationBuilder.DropTable(
            name: "ApplicationLogs",
            schema: "onedrive");
}
