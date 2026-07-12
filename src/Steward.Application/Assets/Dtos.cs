using Steward.Application.AssetTypes;
using Steward.Domain.Enums;
using DriveType = Steward.Domain.Enums.DriveType;

namespace Steward.Application.Assets;

/// Type-specific fields shared by create/update requests, so registry-driven
/// applicability checks can treat both uniformly.
public interface IAssetTypeFields
{
    string? Vin { get; }
    string? Make { get; }
    string? Model { get; }
    string? Color { get; }
    decimal? TrackLengthIn { get; }
    string? Hin { get; }
    string? HullMaterial { get; }
    HullType? HullType { get; }
    DriveType? DriveType { get; }
    string? KeelType { get; }
    decimal? MastHeightFt { get; }
    int? MastCount { get; }
    decimal? LengthFt { get; }
    decimal? BeamFt { get; }
    decimal? BallSizeIn { get; }
    decimal? MaxLoadLbs { get; }
    decimal? InteriorHeightFt { get; }
    decimal? InteriorLengthFt { get; }
    decimal? CuttingWidthIn { get; }
    decimal? MaxPsi { get; }
    decimal? MaxGpm { get; }
    string? EquipmentDescription { get; }
    string? LicensePlate { get; }
}

public record AssetResponse(
    Guid Id,
    Guid HouseholdId,
    AssetCategory Category,
    AssetStructuralType StructuralType,
    string Name,
    string? Description,
    int? Year,
    Guid? CoverPhotoId,
    UsageTrackingMode UsageTrackingMode,
    string? Vin,
    string? Make,
    string? Model,
    string? Color,
    decimal? TrackLengthIn,
    string? Hin,
    string? HullMaterial,
    HullType? HullType,
    DriveType? DriveType,
    string? KeelType,
    decimal? MastHeightFt,
    int? MastCount,
    decimal? LengthFt,
    decimal? BeamFt,
    decimal? BallSizeIn,
    decimal? MaxLoadLbs,
    decimal? InteriorHeightFt,
    decimal? InteriorLengthFt,
    decimal? CuttingWidthIn,
    decimal? MaxPsi,
    decimal? MaxGpm,
    string? EquipmentDescription,
    string? LicensePlate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? Powertrain);

public record CreateAssetRequest(
    AssetCategory Category,
    string Name,
    string? Description,
    int? Year,
    UsageTrackingMode? UsageTrackingMode,
    string? Vin,
    string? Make,
    string? Model,
    string? Color,
    decimal? TrackLengthIn,
    string? Hin,
    string? HullMaterial,
    HullType? HullType,
    DriveType? DriveType,
    string? KeelType,
    decimal? MastHeightFt,
    int? MastCount,
    decimal? LengthFt,
    decimal? BeamFt,
    decimal? BallSizeIn,
    decimal? MaxLoadLbs,
    decimal? InteriorHeightFt,
    decimal? InteriorLengthFt,
    decimal? CuttingWidthIn,
    decimal? MaxPsi,
    decimal? MaxGpm,
    string? EquipmentDescription,
    string? LicensePlate) : IAssetTypeFields;

public record UpdateAssetRequest(
    AssetCategory? Category,
    string Name,
    string? Description,
    int? Year,
    UsageTrackingMode UsageTrackingMode,
    string? Vin,
    string? Make,
    string? Model,
    string? Color,
    decimal? TrackLengthIn,
    string? Hin,
    string? HullMaterial,
    HullType? HullType,
    DriveType? DriveType,
    string? KeelType,
    decimal? MastHeightFt,
    int? MastCount,
    decimal? LengthFt,
    decimal? BeamFt,
    decimal? BallSizeIn,
    decimal? MaxLoadLbs,
    decimal? InteriorHeightFt,
    decimal? InteriorLengthFt,
    decimal? CuttingWidthIn,
    decimal? MaxPsi,
    decimal? MaxGpm,
    string? EquipmentDescription,
    string? LicensePlate) : IAssetTypeFields;
