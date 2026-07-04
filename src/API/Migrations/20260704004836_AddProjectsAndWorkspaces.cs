using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectsAndWorkspaces : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Chats",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WorkspaceId",
                table: "Chats",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workspaces",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Path = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    NormalizedPath = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workspaces", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workspaces_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_ProjectId",
                table: "Chats",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Chats_WorkspaceId",
                table: "Chats",
                column: "WorkspaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_UpdatedAt",
                table: "Projects",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Workspaces_ProjectId_NormalizedPath",
                table: "Workspaces",
                columns: new[] { "ProjectId", "NormalizedPath" },
                unique: true);

            ProjectWorkspaceMigrationBackfill.Apply(migrationBuilder);

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Projects_ProjectId",
                table: "Chats",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Workspaces_WorkspaceId",
                table: "Chats",
                column: "WorkspaceId",
                principalTable: "Workspaces",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Projects_ProjectId",
                table: "Chats");

            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Workspaces_WorkspaceId",
                table: "Chats");

            migrationBuilder.DropTable(
                name: "Workspaces");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Chats_ProjectId",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_WorkspaceId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "WorkspaceId",
                table: "Chats");
        }
    }
}
