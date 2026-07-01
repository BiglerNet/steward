using Steward.Application.Assets;
using Steward.Domain.Common.Exceptions;
using Steward.Domain.Entities.Assets;
using Steward.Domain.Enums;

namespace Steward.Infrastructure.Assets;

internal static class AssetMapper
{
    public static Asset CreateEntity(Guid householdId, CreateAssetRequest request)
    {
        Asset asset = request.AssetType switch
        {
            AssetType.Snowmobile => new Snowmobile { Name = request.Name, TrackLengthIn = request.TrackLengthIn },
            AssetType.Utv => new Utv { Name = request.Name },
            AssetType.Boat => new Boat
            {
                Name = request.Name,
                Hin = request.Hin,
                HullMaterial = request.HullMaterial,
                LengthFt = request.LengthFt,
                BeamFt = request.BeamFt,
            },
            AssetType.Car => new Car { Name = request.Name },
            AssetType.Truck => new Truck { Name = request.Name },
            AssetType.SnowmobileTrailer => new SnowmobileTrailer
            {
                Name = request.Name,
                BallSizeIn = request.BallSizeIn,
                MaxLoadLbs = request.MaxLoadLbs,
            },
            AssetType.EnclosedTrailer => new EnclosedTrailer
            {
                Name = request.Name,
                InteriorHeightFt = request.InteriorHeightFt,
                InteriorLengthFt = request.InteriorLengthFt,
            },
            AssetType.RidingMower => new RidingMower { Name = request.Name, CuttingWidthIn = request.CuttingWidthIn },
            AssetType.PowerWasher => new PowerWasher
            {
                Name = request.Name,
                MaxPsi = request.MaxPsi,
                MaxGpm = request.MaxGpm,
            },
            AssetType.SmallEngine => new SmallEngine
            {
                Name = request.Name,
                EquipmentDescription = request.EquipmentDescription,
            },
            _ => throw new BadRequestException($"Unknown asset type '{request.AssetType}'."),
        };

        asset.Id = Guid.NewGuid();
        asset.HouseholdId = householdId;
        asset.Description = request.Description;
        asset.Year = request.Year;
        asset.PhotoUrl = request.PhotoUrl;
        asset.UsageTrackingMode = request.UsageTrackingMode;

        if (asset is Vehicle vehicle)
        {
            vehicle.Vin = request.Vin;
            vehicle.Color = request.Color;
            vehicle.Make = request.Make;
            vehicle.Model = request.Model;
        }

        return asset;
    }

    public static void ApplyUpdate(Asset asset, UpdateAssetRequest request)
    {
        asset.Name = request.Name;
        asset.Description = request.Description;
        asset.Year = request.Year;
        asset.PhotoUrl = request.PhotoUrl;
        asset.UsageTrackingMode = request.UsageTrackingMode;

        if (asset is Vehicle vehicle)
        {
            vehicle.Vin = request.Vin;
            vehicle.Color = request.Color;
            vehicle.Make = request.Make;
            vehicle.Model = request.Model;
        }

        switch (asset)
        {
            case Boat boat:
                boat.Hin = request.Hin;
                boat.HullMaterial = request.HullMaterial;
                boat.LengthFt = request.LengthFt;
                boat.BeamFt = request.BeamFt;
                break;
            case Snowmobile snowmobile:
                snowmobile.TrackLengthIn = request.TrackLengthIn;
                break;
            case SnowmobileTrailer snowmobileTrailer:
                snowmobileTrailer.BallSizeIn = request.BallSizeIn;
                snowmobileTrailer.MaxLoadLbs = request.MaxLoadLbs;
                break;
            case EnclosedTrailer enclosedTrailer:
                enclosedTrailer.InteriorHeightFt = request.InteriorHeightFt;
                enclosedTrailer.InteriorLengthFt = request.InteriorLengthFt;
                break;
            case RidingMower ridingMower:
                ridingMower.CuttingWidthIn = request.CuttingWidthIn;
                break;
            case PowerWasher powerWasher:
                powerWasher.MaxPsi = request.MaxPsi;
                powerWasher.MaxGpm = request.MaxGpm;
                break;
            case SmallEngine smallEngine:
                smallEngine.EquipmentDescription = request.EquipmentDescription;
                break;
        }
    }

    public static AssetType GetAssetType(Asset asset) => asset switch
    {
        Snowmobile => AssetType.Snowmobile,
        Utv => AssetType.Utv,
        Boat => AssetType.Boat,
        Car => AssetType.Car,
        Truck => AssetType.Truck,
        SnowmobileTrailer => AssetType.SnowmobileTrailer,
        EnclosedTrailer => AssetType.EnclosedTrailer,
        RidingMower => AssetType.RidingMower,
        PowerWasher => AssetType.PowerWasher,
        SmallEngine => AssetType.SmallEngine,
        _ => throw new BadRequestException($"Unknown asset type '{asset.GetType().Name}'."),
    };

    public static AssetResponse ToResponse(Asset asset)
    {
        var vehicle = asset as Vehicle;

        return new AssetResponse(
            asset.Id,
            asset.HouseholdId,
            GetAssetType(asset),
            asset.Name,
            asset.Description,
            asset.Year,
            asset.PhotoUrl,
            asset.UsageTrackingMode,
            vehicle?.Vin,
            vehicle?.Color,
            vehicle?.Make,
            vehicle?.Model,
            (asset as Boat)?.Hin,
            (asset as Boat)?.HullMaterial,
            (asset as Boat)?.LengthFt,
            (asset as Boat)?.BeamFt,
            (asset as Snowmobile)?.TrackLengthIn,
            (asset as SnowmobileTrailer)?.BallSizeIn,
            (asset as SnowmobileTrailer)?.MaxLoadLbs,
            (asset as EnclosedTrailer)?.InteriorHeightFt,
            (asset as EnclosedTrailer)?.InteriorLengthFt,
            (asset as RidingMower)?.CuttingWidthIn,
            (asset as PowerWasher)?.MaxPsi,
            (asset as PowerWasher)?.MaxGpm,
            (asset as SmallEngine)?.EquipmentDescription,
            asset.CreatedAt,
            asset.UpdatedAt);
    }
}
