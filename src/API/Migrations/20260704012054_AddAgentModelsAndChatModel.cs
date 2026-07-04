using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentModelsAndChatModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModelId",
                table: "Chats",
                type: "TEXT",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentModels",
                columns: table => new
                {
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    ModelId = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Label = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsCurrent = table.Column<bool>(type: "INTEGER", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentModels", x => new { x.AgentId, x.ModelId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentModels_AgentId_IsEnabled",
                table: "AgentModels",
                columns: new[] { "AgentId", "IsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentModels");

            migrationBuilder.DropColumn(
                name: "ModelId",
                table: "Chats");
        }
    }
}
