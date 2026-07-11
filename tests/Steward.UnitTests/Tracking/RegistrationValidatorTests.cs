using FluentValidation.TestHelper;
using Steward.Application.Tracking.Registrations;
using Steward.Domain.Enums;

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
    public void Missing_Kind_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { Kind = null });

        result.ShouldHaveValidationErrorFor(x => x.Kind);
    }

    [Fact]
    public void Unknown_Kind_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { Kind = (RegistrationKind)999 });

        result.ShouldHaveValidationErrorFor(x => x.Kind!.Value);
    }

    [Fact]
    public void Null_RegistrationNumber_Passes()
    {
        var result = _createValidator.TestValidate(NewCreate() with { RegistrationNumber = null });

        result.ShouldNotHaveValidationErrorFor(x => x.RegistrationNumber);
    }

    [Fact]
    public void Empty_RegistrationNumber_Passes()
    {
        var result = _createValidator.TestValidate(NewCreate() with { RegistrationNumber = "" });

        result.ShouldNotHaveValidationErrorFor(x => x.RegistrationNumber);
    }

    [Fact]
    public void Overlong_RegistrationNumber_Fails()
    {
        var result = _createValidator.TestValidate(NewCreate() with { RegistrationNumber = new string('A', 101) });

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
    public void Update_Missing_Kind_Fails()
    {
        var result = _updateValidator.TestValidate(NewUpdate() with { Kind = null });

        result.ShouldHaveValidationErrorFor(x => x.Kind);
    }

    [Fact]
    public void Update_Null_RegistrationNumber_Passes()
    {
        var result = _updateValidator.TestValidate(NewUpdate() with { RegistrationNumber = null });

        result.ShouldNotHaveValidationErrorFor(x => x.RegistrationNumber);
    }

    [Fact]
    public void Update_Negative_Cost_Fails()
    {
        var result = _updateValidator.TestValidate(NewUpdate() with { Cost = -1m });

        result.ShouldHaveValidationErrorFor(x => x.Cost);
    }

    private static CreateRegistrationRequest NewCreate() => new(
        Kind: RegistrationKind.Registration,
        RegistrationNumber: "ABC-1234",
        IssuingAuthority: "DMV",
        ValidFrom: new DateOnly(2026, 1, 15),
        RenewedOn: new DateOnly(2026, 1, 15),
        Cost: 120.00m,
        ExpiresOn: new DateOnly(2027, 1, 15),
        Notes: null);

    private static UpdateRegistrationRequest NewUpdate() => new(
        Kind: RegistrationKind.Registration,
        RegistrationNumber: "ABC-1234",
        IssuingAuthority: "DMV",
        ValidFrom: new DateOnly(2026, 1, 15),
        RenewedOn: new DateOnly(2026, 1, 15),
        Cost: 120.00m,
        ExpiresOn: new DateOnly(2027, 1, 15),
        Notes: null);
}
