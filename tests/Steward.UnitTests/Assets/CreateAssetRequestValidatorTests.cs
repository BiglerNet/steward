using FluentValidation.TestHelper;
using Steward.Application.Assets;
using Steward.Domain.Enums;
using DriveType = Steward.Domain.Enums.DriveType;

namespace Steward.UnitTests.Assets;

public class CreateAssetRequestValidatorTests
{
    private readonly CreateAssetRequestValidator _validator = new();

    [Fact]
    public void Valid_Snowmobile_Request_Passes()
    {
        var request = NewRequest(AssetCategory.Snowmobile, "Ski-Doo") with { TrackLengthIn = 136m };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Name_Fails()
    {
        var request = NewRequest(AssetCategory.Snowmobile, "");

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Unknown_Category_Fails()
    {
        var request = NewRequest((AssetCategory)999, "Mystery");

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Theory]
    [InlineData(1899)]
    [InlineData(2200)]
    public void Year_Out_Of_Range_Fails(int year)
    {
        var request = NewRequest(AssetCategory.Snowmobile, "Ski-Doo") with { Year = year };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Year);
    }

    [Fact]
    public void Omitted_UsageTrackingMode_Passes()
    {
        var request = NewRequest(AssetCategory.Car, "Daily Driver");

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PowerBoat_With_NonPositive_LengthFt_Fails()
    {
        var request = NewRequest(AssetCategory.PowerBoat, "Sea Ray") with { LengthFt = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.LengthFt);
    }

    [Fact]
    public void PowerBoat_With_Positive_LengthFt_And_BeamFt_Passes()
    {
        var request = NewRequest(AssetCategory.PowerBoat, "Sea Ray") with { LengthFt = 24.5m, BeamFt = 8.5m };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void PowerBoat_With_DriveType_And_HullType_Passes()
    {
        var request = NewRequest(AssetCategory.PowerBoat, "Sea Ray") with
        {
            HullType = HullType.Monohull,
            DriveType = DriveType.SternDrive,
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void DriveType_On_Sailboat_Fails()
    {
        var request = NewRequest(AssetCategory.Sailboat, "Wind Dancer") with { DriveType = DriveType.Outboard };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("driveType");
    }

    [Fact]
    public void Sailboat_With_Rig_Fields_Passes()
    {
        var request = NewRequest(AssetCategory.Sailboat, "Wind Dancer") with
        {
            HullType = HullType.Monohull,
            KeelType = "Fin",
            MastHeightFt = 42m,
            MastCount = 1,
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Sailboat_With_NonPositive_MastCount_Fails()
    {
        var request = NewRequest(AssetCategory.Sailboat, "Wind Dancer") with { MastCount = 0 };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MastCount);
    }

    [Fact]
    public void Inapplicable_Field_For_Category_Fails_Naming_The_Field()
    {
        var request = NewRequest(AssetCategory.Car, "Daily Driver") with { MaxPsi = 3000m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("maxPsi");
    }

    [Fact]
    public void Vin_On_Trailer_Category_Fails()
    {
        var request = NewRequest(AssetCategory.EnclosedTrailer, "Cargo Trailer") with { Vin = "1FTSW21P34EB12345" };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("vin");
    }

    [Fact]
    public void TrackLengthIn_Applicable_Only_To_Snowmobile()
    {
        var request = NewRequest(AssetCategory.Car, "Daily Driver") with { TrackLengthIn = 136m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("trackLengthIn");
    }

    [Fact]
    public void SnowmobileTrailer_With_NonPositive_BallSizeIn_Fails()
    {
        var request = NewRequest(AssetCategory.SnowmobileTrailer, "Trailer") with { BallSizeIn = -1m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.BallSizeIn);
    }

    [Fact]
    public void EnclosedTrailer_With_NonPositive_InteriorHeightFt_Fails()
    {
        var request = NewRequest(AssetCategory.EnclosedTrailer, "Cargo Trailer") with { InteriorHeightFt = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.InteriorHeightFt);
    }

    [Fact]
    public void RidingMower_With_NonPositive_CuttingWidthIn_Fails()
    {
        var request = NewRequest(AssetCategory.RidingMower, "Mower") with { CuttingWidthIn = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.CuttingWidthIn);
    }

    [Fact]
    public void PowerWasher_With_NonPositive_MaxPsi_Fails()
    {
        var request = NewRequest(AssetCategory.PowerWasher, "Washer") with { MaxPsi = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MaxPsi);
    }

    private static CreateAssetRequest NewRequest(AssetCategory category, string name) => new(
        Category: category,
        Name: name,
        Description: null,
        Year: null,
        UsageTrackingMode: null,
        Vin: null,
        Make: null,
        Model: null,
        Color: null,
        TrackLengthIn: null,
        Hin: null,
        HullMaterial: null,
        HullType: null,
        DriveType: null,
        KeelType: null,
        MastHeightFt: null,
        MastCount: null,
        LengthFt: null,
        BeamFt: null,
        BallSizeIn: null,
        MaxLoadLbs: null,
        InteriorHeightFt: null,
        InteriorLengthFt: null,
        CuttingWidthIn: null,
        MaxPsi: null,
        MaxGpm: null,
        EquipmentDescription: null, LicensePlate: null);
}
