using FluentValidation.TestHelper;
using Steward.Application.Assets;
using Steward.Domain.Enums;

namespace Steward.UnitTests.Assets;

public class UpdateAssetRequestValidatorTests
{
    private readonly UpdateAssetRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_Without_AssetType_Passes()
    {
        var request = NewRequest("Ski-Doo Renamed");

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var request = NewRequest("");

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Unknown_AssetType_Fails()
    {
        var request = NewRequest("Mystery") with { AssetType = (AssetType)999 };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AssetType!.Value);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2200)]
    public void Year_Out_Of_Range_Fails(int year)
    {
        var request = NewRequest("Ski-Doo") with { Year = year };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Boat_With_NonPositive_BeamFt_Fails()
    {
        var request = NewRequest("Sea Ray") with { AssetType = AssetType.Boat, BeamFt = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.BeamFt);
    }

    [Fact]
    public void PowerWasher_With_NonPositive_MaxGpm_Fails()
    {
        var request = NewRequest("Washer") with { AssetType = AssetType.PowerWasher, MaxGpm = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MaxGpm);
    }

    private static UpdateAssetRequest NewRequest(string name) => new(
        AssetType: null,
        Name: name,
        Description: null,
        Year: null,
        PhotoUrl: null,
        UsageTrackingMode: UsageTrackingMode.None,
        Vin: null,
        Color: null,
        Make: null,
        Model: null,
        Hin: null,
        HullMaterial: null,
        LengthFt: null,
        BeamFt: null,
        TrackLengthIn: null,
        BallSizeIn: null,
        MaxLoadLbs: null,
        InteriorHeightFt: null,
        InteriorLengthFt: null,
        CuttingWidthIn: null,
        MaxPsi: null,
        MaxGpm: null,
        EquipmentDescription: null);
}
