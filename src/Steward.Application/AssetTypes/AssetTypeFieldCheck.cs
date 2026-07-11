using Steward.Application.Assets;
using Steward.Domain.Enums;

namespace Steward.Application.AssetTypes;

/// Checks type-specific field values against a category's registry `ApplicableFields`.
/// Used by the create validator and by the asset service on update (where the category
/// comes from the stored asset, not the request).
public static class AssetTypeFieldCheck
{
    private static readonly (string Name, Func<IAssetTypeFields, object?> Get)[] Fields =
    [
        ("vin", x => x.Vin),
        ("make", x => x.Make),
        ("model", x => x.Model),
        ("color", x => x.Color),
        ("trackLengthIn", x => x.TrackLengthIn),
        ("hin", x => x.Hin),
        ("hullMaterial", x => x.HullMaterial),
        ("hullType", x => x.HullType),
        ("driveType", x => x.DriveType),
        ("keelType", x => x.KeelType),
        ("mastHeightFt", x => x.MastHeightFt),
        ("mastCount", x => x.MastCount),
        ("lengthFt", x => x.LengthFt),
        ("beamFt", x => x.BeamFt),
        ("ballSizeIn", x => x.BallSizeIn),
        ("maxLoadLbs", x => x.MaxLoadLbs),
        ("interiorHeightFt", x => x.InteriorHeightFt),
        ("interiorLengthFt", x => x.InteriorLengthFt),
        ("cuttingWidthIn", x => x.CuttingWidthIn),
        ("maxPsi", x => x.MaxPsi),
        ("maxGpm", x => x.MaxGpm),
        ("equipmentDescription", x => x.EquipmentDescription),
        ("licensePlate", x => x.LicensePlate),
    ];

    /// Returns the names of fields that have a value but are not applicable to the category.
    public static IReadOnlyList<string> FindInapplicableFields(AssetCategory category, IAssetTypeFields values)
    {
        var applicable = AssetTypeRegistry.Get(category).ApplicableFields;
        return Fields
            .Where(f => f.Get(values) is not null && !applicable.Contains(f.Name))
            .Select(f => f.Name)
            .ToList();
    }

    public static string InapplicableMessage(string fieldName, AssetCategory category) =>
        $"Field '{fieldName}' is not applicable to category '{category}'.";
}
