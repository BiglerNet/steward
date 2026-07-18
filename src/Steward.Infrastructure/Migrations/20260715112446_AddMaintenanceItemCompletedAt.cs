using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Steward.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceItemCompletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                schema: "steward",
                table: "MaintenanceItems",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                schema: "steward",
                table: "MaintenanceItems");
        }
    }
}
