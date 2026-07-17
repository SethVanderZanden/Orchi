using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Orchi.Api.Data;

#nullable disable

namespace Orchi.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260717180000_AddAgentCliOptions")]
    public partial class AddAgentCliOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalPolicyId",
                table: "Chats",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasoningEffortId",
                table: "Chats",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApprovalPolicyId",
                table: "ModeRuntimeDefaults",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasoningEffortId",
                table: "ModeRuntimeDefaults",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentCliOptions",
                columns: table => new
                {
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Kind = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    OptionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CliValue = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentCliOptions", x => new { x.AgentId, x.Kind, x.OptionId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentCliOptions_AgentId_Kind_IsEnabled",
                table: "AgentCliOptions",
                columns: new[] { "AgentId", "Kind", "IsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AgentCliOptions");

            migrationBuilder.DropColumn(name: "ApprovalPolicyId", table: "Chats");
            migrationBuilder.DropColumn(name: "ReasoningEffortId", table: "Chats");
            migrationBuilder.DropColumn(name: "ApprovalPolicyId", table: "ModeRuntimeDefaults");
            migrationBuilder.DropColumn(name: "ReasoningEffortId", table: "ModeRuntimeDefaults");
        }
    }
}
