using FluentValidation.TestHelper;
using Steward.Application.Assets;
using Steward.Domain.Enums;

namespace Steward.UnitTests.Assets;

public class CreateAssetRequestValidatorTests
{
    private readonly CreateAssetRequestValidator _validator = new();

    [Fact]
    public void Valid_Snowmobile_Request_Passes()
    {
        var request = NewRequest(AssetType.Snowmobile, "Ski-Doo") with { TrackLengthIn = 136m };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var request = NewRequest(AssetType.Snowmobile, "");

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Unknown_AssetType_Fails()
    {
        var request = NewRequest((AssetType)999, "Mystery");

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AssetType);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2200)]
    public void Year_Out_Of_Range_Fails(int year)
    {
        var request = NewRequest(AssetType.Snowmobile, "Ski-Doo") with { Year = year };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Boat_With_NonPositive_LengthFt_Fails()
    {
        var request = NewRequest(AssetType.Boat, "Sea Ray") with { LengthFt = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.LengthFt);
    }

    [Fact]
    public void Boat_With_Positive_LengthFt_And_BeamFt_Passes()
    {
        var request = NewRequest(AssetType.Boat, "Sea Ray") with { LengthFt = 24.5m, BeamFt = 8.5m };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NonPositive_LengthFt_Ignored_For_NonBoat_AssetType()
    {
        var request = NewRequest(AssetType.Snowmobile, "Ski-Doo") with { LengthFt = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.LengthFt);
    }

    [Fact]
    public void SnowmobileTrailer_With_NonPositive_BallSizeIn_Fails()
    {
        var request = NewRequest(AssetType.SnowmobileTrailer, "Trailer") with { BallSizeIn = -1m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.BallSizeIn);
    }

    [Fact]
    public void EnclosedTrailer_With_NonPositive_InteriorHeightFt_Fails()
    {
        var request = NewRequest(AssetType.EnclosedTrailer, "Cargo Trailer") with { InteriorHeightFt = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.InteriorHeightFt);
    }

    [Fact]
    public void RidingMower_With_NonPositive_CuttingWidthIn_Fails()
    {
        var request = NewRequest(AssetType.RidingMower, "Mower") with { CuttingWidthIn = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CuttingWidthIn);
    }

    [Fact]
    public void PowerWasher_With_NonPositive_MaxPsi_Fails()
    {
        var request = NewRequest(AssetType.PowerWasher, "Washer") with { MaxPsi = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MaxPsi);
    }

    private static CreateAssetRequest NewRequest(AssetType assetType, string name) => new(
        AssetType: assetType,
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
