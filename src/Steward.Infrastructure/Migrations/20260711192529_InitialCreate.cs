using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Steward.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "steward");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: true),
                    AvatarUrl = table.Column<string>(type: "text", nullable: true),
                    ThemePreference = table.Column<int>(type: "integer", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "steward",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "steward",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "steward",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "steward",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                schema: "steward",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "steward",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "steward",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "steward",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "steward",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Households",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PublicSlug = table.Column<string>(type: "text", nullable: false),
                    IsPublicVisible = table.Column<bool>(type: "boolean", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: true),
                    Region = table.Column<string>(type: "text", nullable: true),
                    StorageUsedBytes = table.Column<long>(type: "bigint", nullable: false),
                    StorageQuotaOverrideBytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Households", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Households_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "steward",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdDashboards",
                schema: "steward",
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
                        principalSchema: "steward",
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdInvitations",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    InviteCode = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AcceptedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdInvitations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdInvitations_AspNetUsers_AcceptedByUserId",
                        column: x => x.AcceptedByUserId,
                        principalSchema: "steward",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdInvitations_AspNetUsers_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalSchema: "steward",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdInvitations_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "steward",
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HouseholdMemberships",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InvitedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InvitedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HouseholdMemberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HouseholdMemberships_AspNetUsers_InvitedByUserId",
                        column: x => x.InvitedByUserId,
                        principalSchema: "steward",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdMemberships_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "steward",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HouseholdMemberships_Households_HouseholdId",
                        column: x => x.HouseholdId,
                        principalSchema: "steward",
                        principalTable: "Households",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DashboardWidgets",
                schema: "steward",
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
                        principalSchema: "steward",
                        principalTable: "HouseholdDashboards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssetPhotos",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThumbStorageKey = table.Column<string>(type: "text", nullable: false),
                    DisplayStorageKey = table.Column<string>(type: "text", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetPhotos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    CoverPhotoId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsageTrackingMode = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    Hin = table.Column<string>(type: "text", nullable: true),
                    HullMaterial = table.Column<string>(type: "text", nullable: true),
                    HullType = table.Column<int>(type: "integer", nullable: true),
                    DriveType = table.Column<int>(type: "integer", nullable: true),
                    KeelType = table.Column<string>(type: "text", nullable: true),
                    MastHeightFt = table.Column<decimal>(type: "numeric", nullable: true),
                    MastCount = table.Column<int>(type: "integer", nullable: true),
                    LengthFt = table.Column<decimal>(type: "numeric", nullable: true),
                    BeamFt = table.Column<decimal>(type: "numeric", nullable: true),
                    Make = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    Color = table.Column<string>(type: "text", nullable: true),
                    CuttingWidthIn = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxPsi = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxGpm = table.Column<decimal>(type: "numeric", nullable: true),
                    EquipmentDescription = table.Column<string>(type: "text", nullable: true),
                    BallSizeIn = table.Column<decimal>(type: "numeric", nullable: true),
                    MaxLoadLbs = table.Column<decimal>(type: "numeric", nullable: true),
                    InteriorHeightFt = table.Column<decimal>(type: "numeric", nullable: true),
                    InteriorLengthFt = table.Column<decimal>(type: "numeric", nullable: true),
                    LicensePlate = table.Column<string>(type: "text", nullable: true),
                    Vin = table.Column<string>(type: "text", nullable: true),
                    Vehicle_Make = table.Column<string>(type: "text", nullable: true),
                    Vehicle_Model = table.Column<string>(type: "text", nullable: true),
                    Vehicle_Color = table.Column<string>(type: "text", nullable: true),
                    TrackLengthIn = table.Column<decimal>(type: "numeric", nullable: true),
                    Vehicle_LicensePlate = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Assets_AssetPhotos_CoverPhotoId",
                        column: x => x.CoverPhotoId,
                        principalSchema: "steward",
                        principalTable: "AssetPhotos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Engines",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Make = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    SerialNumber = table.Column<string>(type: "text", nullable: true),
                    Year = table.Column<int>(type: "integer", nullable: true),
                    EngineType = table.Column<int>(type: "integer", nullable: false),
                    FuelType = table.Column<int>(type: "integer", nullable: false),
                    Cylinders = table.Column<int>(type: "integer", nullable: true),
                    DisplacementCC = table.Column<decimal>(type: "numeric", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    InstalledDate = table.Column<DateOnly>(type: "date", nullable: true),
                    InstalledAtAssetMiles = table.Column<decimal>(type: "numeric", nullable: true),
                    InstalledAtAssetHours = table.Column<decimal>(type: "numeric", nullable: true),
                    HorsepowerHp = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    TorqueNm = table.Column<decimal>(type: "numeric(8,2)", nullable: true),
                    OilCapacityL = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    RecommendedOilType = table.Column<string>(type: "text", nullable: true),
                    CoolantCapacityL = table.Column<decimal>(type: "numeric(6,3)", nullable: true),
                    RecommendedOctane = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Engines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Engines_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "steward",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MileageLogs",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    OdometerReading = table.Column<decimal>(type: "numeric", nullable: true),
                    TripMiles = table.Column<decimal>(type: "numeric", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MileageLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MileageLogs_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "steward",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Registrations",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    RegistrationNumber = table.Column<string>(type: "text", nullable: true),
                    IssuingAuthority = table.Column<string>(type: "text", nullable: true),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    RenewedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    Cost = table.Column<decimal>(type: "numeric", nullable: true),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    DocumentUrl = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Registrations_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "steward",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Warranties",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiresOn = table.Column<DateOnly>(type: "date", nullable: true),
                    DocumentUrl = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warranties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Warranties_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "steward",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EngineHoursLogs",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    HoursReading = table.Column<decimal>(type: "numeric", nullable: true),
                    TripHours = table.Column<decimal>(type: "numeric", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EngineHoursLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EngineHoursLogs_Engines_EngineId",
                        column: x => x.EngineId,
                        principalSchema: "steward",
                        principalTable: "Engines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FuelLogs",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineId = table.Column<Guid>(type: "uuid", nullable: true),
                    LogType = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Volume = table.Column<decimal>(type: "numeric", nullable: false),
                    VolumeUnit = table.Column<int>(type: "integer", nullable: false),
                    FuelGrade = table.Column<string>(type: "text", nullable: true),
                    PricePerUnit = table.Column<decimal>(type: "numeric", nullable: true),
                    TotalCost = table.Column<decimal>(type: "numeric", nullable: true),
                    MilesAtLog = table.Column<decimal>(type: "numeric", nullable: true),
                    HoursAtLog = table.Column<decimal>(type: "numeric", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelLogs_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "steward",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FuelLogs_Engines_EngineId",
                        column: x => x.EngineId,
                        principalSchema: "steward",
                        principalTable: "Engines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ServiceRecords",
                schema: "steward",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineId = table.Column<Guid>(type: "uuid", nullable: true),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ProviderName = table.Column<string>(type: "text", nullable: true),
                    Cost = table.Column<decimal>(type: "numeric", nullable: true),
                    OdometerMiles = table.Column<decimal>(type: "numeric", nullable: true),
                    EngineHours = table.Column<decimal>(type: "numeric", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "steward",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceRecords_Engines_EngineId",
                        column: x => x.EngineId,
                        principalSchema: "steward",
                        principalTable: "Engines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                schema: "steward",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "steward",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                schema: "steward",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                schema: "steward",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                schema: "steward",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "steward",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "steward",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetPhotos_AssetId",
                schema: "steward",
                table: "AssetPhotos",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_CoverPhotoId",
                schema: "steward",
                table: "Assets",
                column: "CoverPhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_HouseholdId",
                schema: "steward",
                table: "Assets",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_DashboardWidgets_DashboardId",
                schema: "steward",
                table: "DashboardWidgets",
                column: "DashboardId");

            migrationBuilder.CreateIndex(
                name: "IX_EngineHoursLogs_EngineId",
                schema: "steward",
                table: "EngineHoursLogs",
                column: "EngineId");

            migrationBuilder.CreateIndex(
                name: "IX_Engines_AssetId",
                schema: "steward",
                table: "Engines",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelLogs_AssetId",
                schema: "steward",
                table: "FuelLogs",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelLogs_EngineId",
                schema: "steward",
                table: "FuelLogs",
                column: "EngineId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdDashboards_HouseholdId",
                schema: "steward",
                table: "HouseholdDashboards",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdDashboards_HouseholdId_Name",
                schema: "steward",
                table: "HouseholdDashboards",
                columns: new[] { "HouseholdId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdInvitations_AcceptedByUserId",
                schema: "steward",
                table: "HouseholdInvitations",
                column: "AcceptedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdInvitations_Email",
                schema: "steward",
                table: "HouseholdInvitations",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdInvitations_HouseholdId",
                schema: "steward",
                table: "HouseholdInvitations",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdInvitations_InviteCode",
                schema: "steward",
                table: "HouseholdInvitations",
                column: "InviteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdInvitations_InvitedByUserId",
                schema: "steward",
                table: "HouseholdInvitations",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMemberships_HouseholdId_UserId",
                schema: "steward",
                table: "HouseholdMemberships",
                columns: new[] { "HouseholdId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMemberships_InvitedByUserId",
                schema: "steward",
                table: "HouseholdMemberships",
                column: "InvitedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HouseholdMemberships_UserId",
                schema: "steward",
                table: "HouseholdMemberships",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Households_CreatedByUserId",
                schema: "steward",
                table: "Households",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Households_PublicSlug",
                schema: "steward",
                table: "Households",
                column: "PublicSlug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MileageLogs_AssetId",
                schema: "steward",
                table: "MileageLogs",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_AssetId",
                schema: "steward",
                table: "Registrations",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_ExpiresOn",
                schema: "steward",
                table: "Registrations",
                column: "ExpiresOn");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_AssetId",
                schema: "steward",
                table: "ServiceRecords",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceRecords_EngineId",
                schema: "steward",
                table: "ServiceRecords",
                column: "EngineId");

            migrationBuilder.CreateIndex(
                name: "IX_Warranties_AssetId",
                schema: "steward",
                table: "Warranties",
                column: "AssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssetPhotos_Assets_AssetId",
                schema: "steward",
                table: "AssetPhotos",
                column: "AssetId",
                principalSchema: "steward",
                principalTable: "Assets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssetPhotos_Assets_AssetId",
                schema: "steward",
                table: "AssetPhotos");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "DashboardWidgets",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "EngineHoursLogs",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "FuelLogs",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "HouseholdInvitations",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "HouseholdMemberships",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "MileageLogs",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "Registrations",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "ServiceRecords",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "Warranties",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "HouseholdDashboards",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "Engines",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "Households",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "Assets",
                schema: "steward");

            migrationBuilder.DropTable(
                name: "AssetPhotos",
                schema: "steward");
        }
    }
}
