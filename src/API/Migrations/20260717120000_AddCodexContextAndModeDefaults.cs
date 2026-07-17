using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Orchi.Api.Data;

#nullable disable

namespace Orchi.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260717120000_AddCodexContextAndModeDefaults")]
    public partial class AddCodexContextAndModeDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContextSizeId",
                table: "Chats",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentContextSizes",
                columns: table => new
                {
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    SizeId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    TokenCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentContextSizes", x => new { x.AgentId, x.SizeId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentContextSizes_AgentId_IsEnabled",
                table: "AgentContextSizes",
                columns: new[] { "AgentId", "IsEnabled" });

            migrationBuilder.CreateTable(
                name: "ModeRuntimeDefaults",
                columns: table => new
                {
                    Mode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    ContextSizeId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModeRuntimeDefaults", x => x.Mode);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO ModeRuntimeDefaults (Mode, AgentId, ModelId, ContextSizeId, UpdatedAt)
                SELECT Mode, AgentId, ModelId, NULL, UpdatedAt
                FROM AgentModeModelDefaults
                WHERE AgentId = 'cursor'
                AND Mode NOT IN (SELECT Mode FROM ModeRuntimeDefaults);
                """);

            migrationBuilder.Sql(
                """
                INSERT OR IGNORE INTO ModeRuntimeDefaults (Mode, AgentId, ModelId, ContextSizeId, UpdatedAt)
                VALUES
                  ('default', 'cursor', NULL, NULL, datetime('now')),
                  ('orchestration', 'codex', NULL, NULL, datetime('now')),
                  ('review', 'codex', NULL, NULL, datetime('now')),
                  ('implementation', 'codex', NULL, NULL, datetime('now'));
                """);

            migrationBuilder.Sql(
                """
                UPDATE ModeRuntimeDefaults
                SET AgentId = 'codex', ModelId = NULL, ContextSizeId = NULL
                WHERE Mode IN ('orchestration', 'review', 'implementation');
                """);

            migrationBuilder.DropTable(name: "AgentModeModelDefaults");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentModeModelDefaults",
                columns: table => new
                {
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Mode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentModeModelDefaults", x => new { x.AgentId, x.Mode });
                });

            migrationBuilder.Sql(
                """
                INSERT INTO AgentModeModelDefaults (AgentId, Mode, ModelId, UpdatedAt)
                SELECT AgentId, Mode, ModelId, UpdatedAt
                FROM ModeRuntimeDefaults;
                """);

            migrationBuilder.DropTable(name: "ModeRuntimeDefaults");
            migrationBuilder.DropTable(name: "AgentContextSizes");

            migrationBuilder.DropColumn(
                name: "ContextSizeId",
                table: "Chats");
        }
    }
}
