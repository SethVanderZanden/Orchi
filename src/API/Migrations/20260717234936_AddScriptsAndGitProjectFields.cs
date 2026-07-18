using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddScriptsAndGitProjectFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseBranch",
                table: "Workspaces",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Branch",
                table: "Workspaces",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultBaseBranch",
                table: "Projects",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "main");

            migrationBuilder.AddColumn<int>(
                name: "GitHostProvider",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "UseWorktreeOnKickoff",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "Scripts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 128, nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: true),
                    StepsJson = table.Column<string>(type: "TEXT", maxLength: 32000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scripts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scripts_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScriptBindings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ScriptId = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Event = table.Column<int>(type: "INTEGER", nullable: false),
                    ModeFilter = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Enabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    OnError = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScriptBindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScriptBindings_Scripts_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "Scripts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScriptBindings_Event_Enabled_Order",
                table: "ScriptBindings",
                columns: new[] { "Event", "Enabled", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_ScriptBindings_ScriptId",
                table: "ScriptBindings",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_Scripts_ProjectId",
                table: "Scripts",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScriptBindings");

            migrationBuilder.DropTable(
                name: "Scripts");

            migrationBuilder.DropColumn(
                name: "BaseBranch",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "Branch",
                table: "Workspaces");

            migrationBuilder.DropColumn(
                name: "DefaultBaseBranch",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "GitHostProvider",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UseWorktreeOnKickoff",
                table: "Projects");
        }
    }
}
