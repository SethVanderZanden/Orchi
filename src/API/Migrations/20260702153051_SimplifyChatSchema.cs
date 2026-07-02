using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchi.Api.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyChatSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoalJournalEntries");

            migrationBuilder.DropTable(
                name: "SubPlans");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropIndex(
                name: "IX_Chats_ParentChatId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "AttachedPlanId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "GoalChatId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "ParentChatId",
                table: "Chats");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AttachedPlanId",
                table: "Chats",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GoalChatId",
                table: "Chats",
                type: "TEXT",
                nullable: true);

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

            migrationBuilder.CreateTable(
                name: "GoalJournalEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChatId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalJournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoalJournalEntries_Chats_ChatId",
                        column: x => x.ChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentMarkdown = table.Column<string>(type: "TEXT", nullable: false),
                    SourceChatId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SubPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AssignedChatId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ContentMarkdown = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 512, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubPlans_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Chats_ParentChatId",
                table: "Chats",
                column: "ParentChatId");

            migrationBuilder.CreateIndex(
                name: "IX_GoalJournalEntries_ChatId",
                table: "GoalJournalEntries",
                column: "ChatId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_SourceChatId",
                table: "Plans",
                column: "SourceChatId");

            migrationBuilder.CreateIndex(
                name: "IX_SubPlans_PlanId",
                table: "SubPlans",
                column: "PlanId");
        }
    }
}
