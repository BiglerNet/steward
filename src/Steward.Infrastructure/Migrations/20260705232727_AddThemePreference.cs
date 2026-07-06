using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Steward.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddThemePreference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ThemePreference",
                schema: "steward",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThemePreference",
                schema: "steward",
                table: "AspNetUsers");
        }
    }
}
