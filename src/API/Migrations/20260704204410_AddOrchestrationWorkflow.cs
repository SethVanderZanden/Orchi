using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrchestrationWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrchestrationWorkflows",
                columns: table => new
                {
                    ParentChatId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SequencePlanIdsJson = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: false),
                    NextSequenceIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrchestrationWorkflows", x => x.ParentChatId);
                    table.ForeignKey(
                        name: "FK_OrchestrationWorkflows_Chats_ParentChatId",
                        column: x => x.ParentChatId,
                        principalTable: "Chats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrchestrationWorkflows");
        }
    }
}
