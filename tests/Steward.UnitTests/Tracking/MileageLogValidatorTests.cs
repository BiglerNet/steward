using FluentValidation.TestHelper;
using Steward.Application.Tracking.MileageLogs;

namespace Steward.UnitTests.Tracking;

public class MileageLogValidatorTests
{
    private readonly CreateMileageLogRequestValidator _createValidator = new();
    private readonly UpdateMileageLogRequestValidator _updateValidator = new();

    [Fact]
    public void Valid_Create_Request_Passes()
    {
        var result = _createValidator.TestValidate(NewCreate());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Missing_OdometerReading_And_TripMiles_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { OdometerReading = null, TripMiles = null });

        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void TripMiles_Only_Passes()
    {
        var result = _createValidator.TestValidate(NewCreate() with { OdometerReading = null, TripMiles = 50m });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Negative_OdometerReading_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { OdometerReading = -1m });

        result.ShouldHaveValidationErrorFor(x => x.OdometerReading);
    }

    [Fact]
    public void Negative_TripMiles_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { TripMiles = -1m });

        result.ShouldHaveValidationErrorFor(x => x.TripMiles);
    }

    [Fact]
    public void Update_Missing_Both_Fields_Fails()
    {
        var result = _updateValidator.TestValidate(NewUpdate() with { OdometerReading = null, TripMiles = null });

        Assert.True(result.Errors.Count > 0);
    }

    private static CreateMileageLogRequest NewCreate() => new(
        Date: new DateOnly(2026, 6, 1),
        OdometerReading: 12450m,
        TripMiles: null,
        Notes: null);

    private static UpdateMileageLogRequest NewUpdate() => new(
        Date: new DateOnly(2026, 6, 1),
        OdometerReading: 12450m,
        TripMiles: null,
        Notes: null);
}
