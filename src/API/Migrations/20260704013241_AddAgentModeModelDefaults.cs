using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentModeModelDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentModeModelDefaults");
        }
    }
}
