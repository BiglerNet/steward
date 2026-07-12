using FluentValidation.TestHelper;
using Steward.Application.Assets.Engines;
using Steward.Domain.Enums;

namespace Steward.UnitTests.Assets;

public class EngineValidatorTests
{
    private readonly CreateEngineRequestValidator _createValidator = new();
    private readonly UpdateEngineRequestValidator _updateValidator = new();

    [Fact]
    public void Valid_Ice_Request_Passes()
    {
        var result = _createValidator.TestValidate(NewCreate() with
        {
            EngineType = EngineType.Ice,
            Mechanism = Mechanism.TwoStroke,
            FuelType = FuelType.Gasoline,
            TwoStrokeOilDelivery = TwoStrokeOilDelivery.OilInjected,
            TwoStrokeMixRatio = "50:1",
        });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Valid_Electric_Request_Passes()
    {
        var result = _createValidator.TestValidate(NewCreate() with
        {
            EngineType = EngineType.Electric,
            Mechanism = null,
            FuelType = null,
            IsExternallyChargeable = true,
        });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Mechanism_Rejected_On_Electric_Engine()
    {
        var result = _createValidator.TestValidate(NewCreate() with
        {
            EngineType = EngineType.Electric,
            Mechanism = Mechanism.FourStroke,
        });

        result.ShouldHaveValidationErrorFor(x => x.Mechanism);
    }

    [Fact]
    public void FuelType_Rejected_On_Electric_Engine()
    {
        var result = _createValidator.TestValidate(NewCreate() with
        {
            EngineType = EngineType.Electric,
            FuelType = FuelType.Gasoline,
        });

        result.ShouldHaveValidationErrorFor(x => x.FuelType);
    }

    [Fact]
    public void IsExternallyChargeable_Rejected_On_Ice_Engine()
    {
        var result = _createValidator.TestValidate(NewCreate() with
        {
            EngineType = EngineType.Ice,
            IsExternallyChargeable = true,
        });

        result.ShouldHaveValidationErrorFor(x => x.IsExternallyChargeable);
    }

    [Fact]
    public void TwoStrokeOilDelivery_Rejected_Without_TwoStroke_Mechanism()
    {
        var result = _createValidator.TestValidate(NewCreate() with
        {
            EngineType = EngineType.Ice,
            Mechanism = Mechanism.FourStroke,
            TwoStrokeOilDelivery = TwoStrokeOilDelivery.OilInjected,
        });

        result.ShouldHaveValidationErrorFor(x => x.TwoStrokeOilDelivery);
    }

    [Fact]
    public void TwoStrokeMixRatio_Rejected_Without_TwoStroke_Mechanism()
    {
        var result = _createValidator.TestValidate(NewCreate() with
        {
            EngineType = EngineType.Ice,
            Mechanism = Mechanism.FourStroke,
            TwoStrokeMixRatio = "50:1",
        });

        result.ShouldHaveValidationErrorFor(x => x.TwoStrokeMixRatio);
    }

    [Fact]
    public void Update_Mechanism_Rejected_On_Electric_Engine()
    {
        var result = _updateValidator.TestValidate(NewUpdate() with
        {
            EngineType = EngineType.Electric,
            Mechanism = Mechanism.FourStroke,
        });

        result.ShouldHaveValidationErrorFor(x => x.Mechanism);
    }

    private static CreateEngineRequest NewCreate() => new(
        Label: "Main engine",
        Make: null,
        Model: null,
        SerialNumber: null,
        Year: null,
        EngineType: EngineType.Ice,
        Mechanism: null,
        FuelType: null,
        IsExternallyChargeable: null,
        TwoStrokeOilDelivery: null,
        TwoStrokeMixRatio: null,
        Cylinders: null,
        DisplacementCc: null,
        InstalledDate: null,
        InstalledAtAssetMiles: null,
        InstalledAtAssetHours: null,
        HorsepowerHp: null,
        TorqueNm: null,
        OilCapacityL: null,
        RecommendedOilType: null,
        CoolantCapacityL: null,
        RecommendedOctane: null);

    private static UpdateEngineRequest NewUpdate() => new(
        Label: "Main engine",
        Make: null,
        Model: null,
        SerialNumber: null,
        Year: null,
        EngineType: EngineType.Ice,
        Mechanism: null,
        FuelType: null,
        IsExternallyChargeable: null,
        TwoStrokeOilDelivery: null,
        TwoStrokeMixRatio: null,
        Cylinders: null,
        DisplacementCc: null,
        InstalledDate: null,
        InstalledAtAssetMiles: null,
        InstalledAtAssetHours: null,
        HorsepowerHp: null,
        TorqueNm: null,
        OilCapacityL: null,
        RecommendedOilType: null,
        CoolantCapacityL: null,
        RecommendedOctane: null);
}
