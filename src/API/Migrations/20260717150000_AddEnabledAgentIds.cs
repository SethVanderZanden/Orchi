using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Orchi.Api.Data;

#nullable disable

namespace Orchi.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260717150000_AddEnabledAgentIds")]
    public partial class AddEnabledAgentIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EnabledAgentIdsJson",
                table: "UserPreferences",
                type: "TEXT",
                maxLength: 512,
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnabledAgentIdsJson",
                table: "UserPreferences");
        }
    }
}
