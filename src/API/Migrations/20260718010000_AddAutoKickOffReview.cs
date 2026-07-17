using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Orchi.Api.Data;

#nullable disable

namespace Orchi.Api.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260718010000_AddAutoKickOffReview")]
    public partial class AddAutoKickOffReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoKickOffReview",
                table: "UserPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoKickOffReview",
                table: "UserPreferences");
        }
    }
}
