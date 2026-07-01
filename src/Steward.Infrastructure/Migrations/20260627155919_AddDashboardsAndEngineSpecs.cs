using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Steward.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardsAndEngineSpecs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "CoolantCapacityL",
                table: "Engines",
                type: "numeric(6,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HorsepowerHp",
                table: "Engines",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OilCapacityL",
                table: "Engines",
                type: "numeric(6,3)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecommendedOctane",
                table: "Engines",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedOilType",
                table: "Engines",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TorqueNm",
                table: "Engines",
                type: "numeric(8,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HouseholdDashboards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdDashboards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdDashboards_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DashboardWidgets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DashboardId = table.Column<Guid>(type: "uuid", nullable: false),
                    WidgetType = table.Column<int>(type: "integer", nullable: false),
                    WidgetSize = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Config = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardWidgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DashboardWidgets_HouseholdDashboards_DashboardId",
                        column: x => x.DashboardId,
                        principalTable: "HouseholdDashboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardWidgets_DashboardId",
                table: "DashboardWidgets",
                column: "DashboardId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdDashboards_HouseholdId",
                table: "HouseholdDashboards",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdDashboards_HouseholdId_Name",
                table: "HouseholdDashboards",
                columns: new[] { "HouseholdId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DashboardWidgets");

            migrationBuilder.DropTable(
                name: "HouseholdDashboards");

            migrationBuilder.DropColumn(
                name: "CoolantCapacityL",
                table: "Engines");

            migrationBuilder.DropColumn(
                name: "HorsepowerHp",
                table: "Engines");

            migrationBuilder.DropColumn(
                name: "OilCapacityL",
                table: "Engines");

            migrationBuilder.DropColumn(
                name: "RecommendedOctane",
                table: "Engines");

            migrationBuilder.DropColumn(
                name: "RecommendedOilType",
                table: "Engines");

            migrationBuilder.DropColumn(
                name: "TorqueNm",
                table: "Engines");
        }
    }
}
