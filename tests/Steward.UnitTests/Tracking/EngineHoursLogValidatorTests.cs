using FluentValidation.TestHelper;
using Steward.Application.Tracking.EngineHoursLogs;

namespace Steward.UnitTests.Tracking;

public class EngineHoursLogValidatorTests
{
    private readonly CreateEngineHoursLogRequestValidator _createValidator = new();
    private readonly UpdateEngineHoursLogRequestValidator _updateValidator = new();

    [Fact]
    public void Valid_Create_Request_Passes()
    {
        var result = _createValidator.TestValidate(NewCreate());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Missing_HoursReading_And_TripHours_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { HoursReading = null, TripHours = null });

        Assert.True(result.Errors.Count > 0);
    }

    [Fact]
    public void TripHours_Only_Passes()
    {
        var result = _createValidator.TestValidate(NewCreate() with { HoursReading = null, TripHours = 5m });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Negative_HoursReading_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { HoursReading = -1m });

        result.ShouldHaveValidationErrorFor(x => x.HoursReading);
    }

    [Fact]
    public void Negative_TripHours_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { TripHours = -1m });

        result.ShouldHaveValidationErrorFor(x => x.TripHours);
    }

    [Fact]
    public void Update_Missing_Both_Fields_Fails()
    {
        var result = _updateValidator.TestValidate(NewUpdate() with { HoursReading = null, TripHours = null });

        Assert.True(result.Errors.Count > 0);
    }

    private static CreateEngineHoursLogRequest NewCreate() => new(
        Date: new DateOnly(2026, 6, 1),
        HoursReading: 340.5m,
        TripHours: null,
        Notes: null);

    private static UpdateEngineHoursLogRequest NewUpdate() => new(
        Date: new DateOnly(2026, 6, 1),
        HoursReading: 340.5m,
        TripHours: null,
        Notes: null);
}
