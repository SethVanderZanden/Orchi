using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddChatAgentMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "Chats",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentChatId",
                table: "Chats",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanFilePath",
                table: "Chats",
                type: "TEXT",
                maxLength: 512,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chats_ParentChatId",
                table: "Chats",
                column: "ParentChatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Chats_ParentChatId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "ParentChatId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "PlanFilePath",
                table: "Chats");
        }
    }
}
