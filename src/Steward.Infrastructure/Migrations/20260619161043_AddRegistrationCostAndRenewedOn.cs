using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Steward.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationCostAndRenewedOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Registrations",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "RenewedOn",
                table: "Registrations",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_ExpiresOn",
                table: "Registrations",
                column: "ExpiresOn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Registrations_ExpiresOn",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "RenewedOn",
                table: "Registrations");
        }
    }
}
