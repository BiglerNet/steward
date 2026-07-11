using Steward.Application.AssetTypes;
using Steward.Domain.Enums;

namespace Steward.UnitTests.Assets;

public class AssetTypeRegistryTests
{
    private static readonly IReadOnlyDictionary<AssetStructuralType, string[]> StructuralFields =
        new Dictionary<AssetStructuralType, string[]>
        {
            [AssetStructuralType.Vehicle] = ["vin", "make", "model", "color", "trackLengthIn", "licensePlate"],
            [AssetStructuralType.Boat] = ["hin", "hullMaterial", "hullType", "driveType", "keelType", "mastHeightFt", "mastCount", "lengthFt", "beamFt", "make", "model", "color"],
            [AssetStructuralType.Trailer] = ["ballSizeIn", "maxLoadLbs", "interiorHeightFt", "interiorLengthFt", "licensePlate"],
            [AssetStructuralType.Equipment] = ["cuttingWidthIn", "maxPsi", "maxGpm", "equipmentDescription"],
        };

    [Fact]
    public void Every_Category_Has_Exactly_One_Registry_Entry()
    {
        var categories = Enum.GetValues<AssetCategory>();

        Assert.Equal(categories.Length, AssetTypeRegistry.All.Count);
        Assert.All(categories, category =>
            Assert.Single(AssetTypeRegistry.All, d => d.Category == category));
    }

    [Fact]
    public void Registry_Has_No_Entries_For_Undefined_Categories()
    {
        Assert.All(AssetTypeRegistry.All, d => Assert.True(Enum.IsDefined(d.Category)));
    }

    [Fact]
    public void Applicable_Fields_Exist_On_The_Structural_Type()
    {
        Assert.All(AssetTypeRegistry.All, definition =>
        {
            var structuralFields = StructuralFields[definition.StructuralType];
            Assert.All(definition.ApplicableFields, field => Assert.Contains(field, structuralFields));
        });
    }

    [Fact]
    public void Every_Entry_Has_Label_And_Icon()
    {
        Assert.All(AssetTypeRegistry.All, d =>
        {
            Assert.False(string.IsNullOrWhiteSpace(d.DisplayLabel));
            Assert.False(string.IsNullOrWhiteSpace(d.Icon));
            Assert.Matches("^[a-z0-9]+(-[a-z0-9]+)*$", d.Icon);
        });
    }

    [Fact]
    public void Typical_Permit_Kinds_All_Parse_To_RegistrationKind_Members()
    {
        Assert.All(AssetTypeRegistry.All, d =>
            Assert.All(d.TypicalPermitKinds, kind =>
                Assert.True(Enum.TryParse<RegistrationKind>(kind, out _), $"'{kind}' is not a RegistrationKind member")));
    }
}
