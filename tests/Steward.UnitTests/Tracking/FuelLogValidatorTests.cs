using FluentValidation.TestHelper;
using Steward.Application.Tracking.FuelLogs;
using Steward.Domain.Enums;

namespace Steward.UnitTests.Tracking;

public class FuelLogValidatorTests
{
    private readonly CreateFuelLogRequestValidator _createValidator = new();
    private readonly UpdateFuelLogRequestValidator _updateValidator = new();

    [Fact]
    public void Valid_Create_Request_Passes()
    {
        var result = _createValidator.TestValidate(NewCreate());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Unknown_LogType_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { LogType = (FuelLogType)999 });

        result.ShouldHaveValidationErrorFor(x => x.LogType);
    }

    [Fact]
    public void Unknown_VolumeUnit_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { VolumeUnit = (VolumeUnit)999 });

        result.ShouldHaveValidationErrorFor(x => x.VolumeUnit);
    }

    [Fact]
    public void Negative_Volume_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { Volume = -1m });

        result.ShouldHaveValidationErrorFor(x => x.Volume);
    }

    [Fact]
    public void Negative_TotalCost_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { TotalCost = -1m });

        result.ShouldHaveValidationErrorFor(x => x.TotalCost);
    }

    [Fact]
    public void Update_Unknown_LogType_Fails()
    {
        var result = _updateValidator.TestValidate(NewUpdate() with { LogType = (FuelLogType)999 });

        result.ShouldHaveValidationErrorFor(x => x.LogType);
    }

    private static CreateFuelLogRequest NewCreate() => new(
        LogType: FuelLogType.Fillup,
        Date: new DateOnly(2026, 6, 1),
        Volume: 12.5m,
        VolumeUnit: VolumeUnit.Gallons,
        FuelGrade: null,
        PricePerUnit: null,
        TotalCost: 48.75m,
        MilesAtLog: null,
        HoursAtLog: null,
        EngineId: null,
        Notes: null);

    private static UpdateFuelLogRequest NewUpdate() => new(
        LogType: FuelLogType.Fillup,
        Date: new DateOnly(2026, 6, 1),
        Volume: 12.5m,
        VolumeUnit: VolumeUnit.Gallons,
        FuelGrade: null,
        PricePerUnit: null,
        TotalCost: 48.75m,
        MilesAtLog: null,
        HoursAtLog: null,
        EngineId: null,
        Notes: null);
}
