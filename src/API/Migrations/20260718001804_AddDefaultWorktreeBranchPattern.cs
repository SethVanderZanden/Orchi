using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Orchi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultWorktreeBranchPattern : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DefaultWorktreeBranchPattern",
                table: "Projects",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "orchi/{date}-{shortId}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultWorktreeBranchPattern",
                table: "Projects");
        }
    }
}
