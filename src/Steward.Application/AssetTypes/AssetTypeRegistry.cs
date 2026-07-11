using Steward.Domain.Enums;

namespace Steward.Application.AssetTypes;

/// Single source of truth for per-category asset metadata: grouping, structural class,
/// field applicability, and behavior defaults. Served to the frontend via GET /api/asset-types.
/// Every AssetCategory value must have exactly one entry (enforced by unit test).
public static class AssetTypeRegistry
{
    public const string PermitKindRegistration = "Registration";
    public const string PermitKindTrailPass = "TrailPass";

    private static readonly string[] VehicleFields = ["vin", "make", "model", "color"];
    private static readonly string[] RoadVehicleFields = ["vin", "make", "model", "color", "licensePlate"];
    private static readonly string[] SnowmobileFields = ["vin", "make", "model", "color", "trackLengthIn"];
    private static readonly string[] PwcFields = ["hin", "hullMaterial", "lengthFt", "beamFt", "make", "model", "color"];
    private static readonly string[] PowerBoatFields = ["hin", "hullMaterial", "hullType", "driveType", "lengthFt", "beamFt", "make", "model", "color"];
    private static readonly string[] SailboatFields = ["hin", "hullMaterial", "hullType", "keelType", "mastHeightFt", "mastCount", "lengthFt", "beamFt", "make", "model", "color"];
    private static readonly string[] OpenTrailerFields = ["ballSizeIn", "maxLoadLbs", "licensePlate"];
    private static readonly string[] EnclosedTrailerFields = ["ballSizeIn", "maxLoadLbs", "interiorHeightFt", "interiorLengthFt", "licensePlate"];

    private static readonly string[] RegistrationOnly = [PermitKindRegistration];
    private static readonly string[] RegistrationAndTrailPass = [PermitKindRegistration, PermitKindTrailPass];
    private static readonly string[] NoPermits = [];

    // Icon names are kebab-case lucide-react component names; the frontend maps them via
    // an explicit Record<string, LucideIcon> with a neutral fallback for unrecognized names.
    public static readonly IReadOnlyList<AssetTypeDefinition> All =
    [
        new(AssetCategory.Car, AssetGroup.Road, AssetStructuralType.Vehicle, "Car",
            UsageTrackingMode.Mileage, TypicallyHasEngine: true, VinDecodeSupport.Supported,
            RegistrationOnly, RoadVehicleFields, "car"),
        new(AssetCategory.Truck, AssetGroup.Road, AssetStructuralType.Vehicle, "Truck",
            UsageTrackingMode.Mileage, TypicallyHasEngine: true, VinDecodeSupport.Supported,
            RegistrationOnly, RoadVehicleFields, "truck"),
        new(AssetCategory.Suv, AssetGroup.Road, AssetStructuralType.Vehicle, "SUV",
            UsageTrackingMode.Mileage, TypicallyHasEngine: true, VinDecodeSupport.Supported,
            RegistrationOnly, RoadVehicleFields, "car-front"),
        new(AssetCategory.Van, AssetGroup.Road, AssetStructuralType.Vehicle, "Van",
            UsageTrackingMode.Mileage, TypicallyHasEngine: true, VinDecodeSupport.Supported,
            RegistrationOnly, RoadVehicleFields, "van"),
        new(AssetCategory.Motorcycle, AssetGroup.Road, AssetStructuralType.Vehicle, "Motorcycle",
            UsageTrackingMode.Mileage, TypicallyHasEngine: true, VinDecodeSupport.Supported,
            RegistrationOnly, RoadVehicleFields, "bike"),

        new(AssetCategory.Utv, AssetGroup.Powersport, AssetStructuralType.Vehicle, "UTV",
            UsageTrackingMode.Both, TypicallyHasEngine: true, VinDecodeSupport.BestEffort,
            RegistrationAndTrailPass, VehicleFields, "car-taxi-front"),
        new(AssetCategory.Atv, AssetGroup.Powersport, AssetStructuralType.Vehicle, "ATV",
            UsageTrackingMode.Both, TypicallyHasEngine: true, VinDecodeSupport.BestEffort,
            RegistrationAndTrailPass, VehicleFields, "mountain"),
        new(AssetCategory.Snowmobile, AssetGroup.Powersport, AssetStructuralType.Vehicle, "Snowmobile",
            UsageTrackingMode.Both, TypicallyHasEngine: true, VinDecodeSupport.BestEffort,
            RegistrationAndTrailPass, SnowmobileFields, "snowflake"),
        new(AssetCategory.DirtBike, AssetGroup.Powersport, AssetStructuralType.Vehicle, "Dirt Bike",
            UsageTrackingMode.Both, TypicallyHasEngine: true, VinDecodeSupport.BestEffort,
            RegistrationAndTrailPass, VehicleFields, "bike"),
        new(AssetCategory.GolfCart, AssetGroup.Powersport, AssetStructuralType.Vehicle, "Golf Cart",
            UsageTrackingMode.Hours, TypicallyHasEngine: true, VinDecodeSupport.None,
            NoPermits, VehicleFields, "battery-charging"),

        new(AssetCategory.PowerBoat, AssetGroup.Water, AssetStructuralType.Boat, "Power Boat",
            UsageTrackingMode.Both, TypicallyHasEngine: true, VinDecodeSupport.None,
            RegistrationOnly, PowerBoatFields, "ship"),
        new(AssetCategory.Sailboat, AssetGroup.Water, AssetStructuralType.Boat, "Sailboat",
            UsageTrackingMode.Hours, TypicallyHasEngine: false, VinDecodeSupport.None,
            RegistrationOnly, SailboatFields, "sailboat"),
        new(AssetCategory.Pwc, AssetGroup.Water, AssetStructuralType.Boat, "Personal Watercraft",
            UsageTrackingMode.Hours, TypicallyHasEngine: true, VinDecodeSupport.None,
            RegistrationOnly, PwcFields, "ship"),

        new(AssetCategory.UtilityTrailer, AssetGroup.Trailer, AssetStructuralType.Trailer, "Utility Trailer",
            UsageTrackingMode.None, TypicallyHasEngine: false, VinDecodeSupport.None,
            RegistrationOnly, OpenTrailerFields, "container"),
        new(AssetCategory.EnclosedTrailer, AssetGroup.Trailer, AssetStructuralType.Trailer, "Enclosed Trailer",
            UsageTrackingMode.None, TypicallyHasEngine: false, VinDecodeSupport.None,
            RegistrationOnly, EnclosedTrailerFields, "caravan"),
        new(AssetCategory.SnowmobileTrailer, AssetGroup.Trailer, AssetStructuralType.Trailer, "Snowmobile Trailer",
            UsageTrackingMode.None, TypicallyHasEngine: false, VinDecodeSupport.None,
            RegistrationOnly, OpenTrailerFields, "package"),
        new(AssetCategory.BoatTrailer, AssetGroup.Trailer, AssetStructuralType.Trailer, "Boat Trailer",
            UsageTrackingMode.None, TypicallyHasEngine: false, VinDecodeSupport.None,
            RegistrationOnly, OpenTrailerFields, "anchor"),

        new(AssetCategory.RidingMower, AssetGroup.Equipment, AssetStructuralType.Equipment, "Riding Mower",
            UsageTrackingMode.Hours, TypicallyHasEngine: true, VinDecodeSupport.None,
            NoPermits, ["cuttingWidthIn"], "tractor"),
        new(AssetCategory.PowerWasher, AssetGroup.Equipment, AssetStructuralType.Equipment, "Power Washer",
            UsageTrackingMode.Hours, TypicallyHasEngine: true, VinDecodeSupport.None,
            NoPermits, ["maxPsi", "maxGpm"], "spray-can"),
        new(AssetCategory.Generator, AssetGroup.Equipment, AssetStructuralType.Equipment, "Generator",
            UsageTrackingMode.Hours, TypicallyHasEngine: true, VinDecodeSupport.None,
            NoPermits, ["equipmentDescription"], "zap"),
        new(AssetCategory.SmallEngine, AssetGroup.Equipment, AssetStructuralType.Equipment, "Small Engine",
            UsageTrackingMode.Hours, TypicallyHasEngine: true, VinDecodeSupport.None,
            NoPermits, ["equipmentDescription"], "cog"),
    ];

    private static readonly IReadOnlyDictionary<AssetCategory, AssetTypeDefinition> ByCategory =
        All.ToDictionary(d => d.Category);

    public static AssetTypeDefinition Get(AssetCategory category) => ByCategory[category];

    public static IReadOnlyList<AssetCategory> CategoriesInGroup(AssetGroup group) =>
        All.Where(d => d.Group == group).Select(d => d.Category).ToList();
}
