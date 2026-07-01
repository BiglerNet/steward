using FluentValidation.TestHelper;
using Steward.Application.Tracking.ServiceRecords;

namespace Steward.UnitTests.Tracking;

public class ServiceRecordValidatorTests
{
    private readonly CreateServiceRecordRequestValidator _createValidator = new();
    private readonly UpdateServiceRecordRequestValidator _updateValidator = new();

    [Fact]
    public void Valid_Create_Request_Passes()
    {
        var result = _createValidator.TestValidate(NewCreate());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_Description_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { Description = "" });

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Negative_Cost_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { Cost = -1m });

        result.ShouldHaveValidationErrorFor(x => x.Cost);
    }

    [Fact]
    public void Negative_OdometerMiles_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { OdometerMiles = -1m });

        result.ShouldHaveValidationErrorFor(x => x.OdometerMiles);
    }

    [Fact]
    public void Negative_EngineHours_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { EngineHours = -1m });

        result.ShouldHaveValidationErrorFor(x => x.EngineHours);
    }

    [Fact]
    public void Update_Empty_Description_Fails()
    {
        var result = _updateValidator.TestValidate(NewUpdate() with { Description = "" });

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    private static CreateServiceRecordRequest NewCreate() => new(
        Date: new DateOnly(2026, 6, 1),
        Description: "Oil change",
        ProviderName: null,
        Cost: 85.00m,
        OdometerMiles: null,
        EngineHours: null,
        EngineId: null,
        Notes: null);

    private static UpdateServiceRecordRequest NewUpdate() => new(
        Date: new DateOnly(2026, 6, 1),
        Description: "Oil change",
        ProviderName: null,
        Cost: 85.00m,
        OdometerMiles: null,
        EngineHours: null,
        EngineId: null,
        Notes: null);
}
