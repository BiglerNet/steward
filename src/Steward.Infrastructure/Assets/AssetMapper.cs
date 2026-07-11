using Steward.Application.Assets;
using Steward.Application.AssetTypes;
using Steward.Domain.Entities.Assets;

namespace Steward.Infrastructure.Assets;

internal static class AssetMapper
{
    public static Asset CreateEntity(Guid householdId, CreateAssetRequest request)
    {
        var definition = AssetTypeRegistry.Get(request.Category);

        Asset asset = definition.StructuralType switch
        {
            AssetStructuralType.Vehicle => new Vehicle { Name = request.Name },
            AssetStructuralType.Boat => new Boat { Name = request.Name },
            AssetStructuralType.Trailer => new Trailer { Name = request.Name },
            AssetStructuralType.Equipment => new Equipment { Name = request.Name },
            _ => throw new InvalidOperationException(
                $"Unknown structural type '{definition.StructuralType}' for category '{request.Category}'."),
        };

        asset.Id = Guid.NewGuid();
        asset.HouseholdId = householdId;
        asset.Category = request.Category;
        asset.Description = request.Description;
        asset.Year = request.Year;
        asset.UsageTrackingMode = request.UsageTrackingMode ?? definition.DefaultUsageTrackingMode;

        ApplyTypeFields(asset, request);

        return asset;
    }

    public static void ApplyUpdate(Asset asset, UpdateAssetRequest request)
    {
        asset.Name = request.Name;
        asset.Description = request.Description;
        asset.Year = request.Year;
        asset.UsageTrackingMode = request.UsageTrackingMode;

        ApplyTypeFields(asset, request);
    }

    public static AssetResponse ToResponse(Asset asset)
    {
        var vehicle = asset as Vehicle;
        var boat = asset as Boat;
        var trailer = asset as Trailer;
        var equipment = asset as Equipment;

        return new AssetResponse(
            asset.Id,
            asset.HouseholdId,
            asset.Category,
            AssetTypeRegistry.Get(asset.Category).StructuralType,
            asset.Name,
            asset.Description,
            asset.Year,
            asset.CoverPhotoId,
            asset.UsageTrackingMode,
            vehicle?.Vin,
            vehicle?.Make ?? boat?.Make,
            vehicle?.Model ?? boat?.Model,
            vehicle?.Color ?? boat?.Color,
            vehicle?.TrackLengthIn,
            boat?.Hin,
            boat?.HullMaterial,
            boat?.HullType,
            boat?.DriveType,
            boat?.KeelType,
            boat?.MastHeightFt,
            boat?.MastCount,
            boat?.LengthFt,
            boat?.BeamFt,
            trailer?.BallSizeIn,
            trailer?.MaxLoadLbs,
            trailer?.InteriorHeightFt,
            trailer?.InteriorLengthFt,
            equipment?.CuttingWidthIn,
            equipment?.MaxPsi,
            equipment?.MaxGpm,
            equipment?.EquipmentDescription,
            vehicle?.LicensePlate ?? trailer?.LicensePlate,
            asset.CreatedAt,
            asset.UpdatedAt);
    }

    private static void ApplyTypeFields(Asset asset, IAssetTypeFields fields)
    {
        switch (asset)
        {
            case Vehicle vehicle:
                vehicle.Vin = fields.Vin;
                vehicle.Make = fields.Make;
                vehicle.Model = fields.Model;
                vehicle.Color = fields.Color;
                vehicle.TrackLengthIn = fields.TrackLengthIn;
                vehicle.LicensePlate = fields.LicensePlate;
                break;
            case Boat boat:
                boat.Hin = fields.Hin;
                boat.HullMaterial = fields.HullMaterial;
                boat.HullType = fields.HullType;
                boat.DriveType = fields.DriveType;
                boat.KeelType = fields.KeelType;
                boat.MastHeightFt = fields.MastHeightFt;
                boat.MastCount = fields.MastCount;
                boat.LengthFt = fields.LengthFt;
                boat.BeamFt = fields.BeamFt;
                boat.Make = fields.Make;
                boat.Model = fields.Model;
                boat.Color = fields.Color;
                break;
            case Trailer trailer:
                trailer.BallSizeIn = fields.BallSizeIn;
                trailer.MaxLoadLbs = fields.MaxLoadLbs;
                trailer.InteriorHeightFt = fields.InteriorHeightFt;
                trailer.InteriorLengthFt = fields.InteriorLengthFt;
                trailer.LicensePlate = fields.LicensePlate;
                break;
            case Equipment equipment:
                equipment.CuttingWidthIn = fields.CuttingWidthIn;
                equipment.MaxPsi = fields.MaxPsi;
                equipment.MaxGpm = fields.MaxGpm;
                equipment.EquipmentDescription = fields.EquipmentDescription;
                break;
        }
    }
}
