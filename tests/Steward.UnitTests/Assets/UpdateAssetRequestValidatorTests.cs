using FluentValidation.TestHelper;
using Steward.Application.Assets;
using Steward.Domain.Enums;

namespace Steward.UnitTests.Assets;

public class UpdateAssetRequestValidatorTests
{
    private readonly UpdateAssetRequestValidator _validator = new();

    [Fact]
    public void Valid_Request_Without_Category_Passes()
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
    public void Unknown_Category_Fails()
    {
        var request = NewRequest("Mystery") with { Category = (AssetCategory)999 };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Category!.Value);
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
    public void NonPositive_BeamFt_Fails()
    {
        var request = NewRequest("Sea Ray") with { BeamFt = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.BeamFt);
    }

    [Fact]
    public void NonPositive_MaxGpm_Fails()
    {
        var request = NewRequest("Washer") with { MaxGpm = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MaxGpm);
    }

    [Fact]
    public void NonPositive_MastHeightFt_Fails()
    {
        var request = NewRequest("Wind Dancer") with { MastHeightFt = 0m };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.MastHeightFt);
    }

    [Fact]
    public void Invalid_HullType_Fails()
    {
        var request = NewRequest("Wind Dancer") with { HullType = (HullType)999 };

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.HullType!.Value);
    }

    private static UpdateAssetRequest NewRequest(string name) => new(
        Category: null,
        Name: name,
        Description: null,
        Year: null,
        UsageTrackingMode: UsageTrackingMode.None,
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
