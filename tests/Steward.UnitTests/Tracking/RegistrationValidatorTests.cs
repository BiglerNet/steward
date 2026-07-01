using FluentValidation.TestHelper;
using Steward.Application.Tracking.Registrations;

namespace Steward.UnitTests.Tracking;

public class RegistrationValidatorTests
{
    private readonly CreateRegistrationRequestValidator _createValidator = new();
    private readonly UpdateRegistrationRequestValidator _updateValidator = new();

    [Fact]
    public void Valid_Create_Request_Passes()
    {
        var result = _createValidator.TestValidate(NewCreate());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Empty_RegistrationNumber_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { RegistrationNumber = "" });

        result.ShouldHaveValidationErrorFor(x => x.RegistrationNumber);
    }

    [Fact]
    public void Negative_Cost_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { Cost = -1m });

        result.ShouldHaveValidationErrorFor(x => x.Cost);
    }

    [Fact]
    public void Null_Cost_Passes()
    {
        var result = _createValidator.TestValidate(NewCreate() with { Cost = null });

        result.ShouldNotHaveValidationErrorFor(x => x.Cost);
    }

    [Fact]
    public void Update_Empty_RegistrationNumber_Fails()
    {
        var result = _updateValidator.TestValidate(NewUpdate() with { RegistrationNumber = "" });

        result.ShouldHaveValidationErrorFor(x => x.RegistrationNumber);
    }

    [Fact]
    public void Update_Negative_Cost_Fails()
    {
        var result = _updateValidator.TestValidate(NewUpdate() with { Cost = -1m });

        result.ShouldHaveValidationErrorFor(x => x.Cost);
    }

    private static CreateRegistrationRequest NewCreate() => new(
        RegistrationNumber: "ABC-1234",
        IssuingAuthority: "DMV",
        RenewedOn: new DateOnly(2026, 1, 15),
        Cost: 120.00m,
        ExpiresOn: new DateOnly(2027, 1, 15),
        Notes: null);

    private static UpdateRegistrationRequest NewUpdate() => new(
        RegistrationNumber: "ABC-1234",
        IssuingAuthority: "DMV",
        RenewedOn: new DateOnly(2026, 1, 15),
        Cost: 120.00m,
        ExpiresOn: new DateOnly(2027, 1, 15),
        Notes: null);
}
