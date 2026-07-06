using FluentValidation.TestHelper;
using Steward.Application.Auth;
using Steward.Domain.Enums;

namespace Steward.UnitTests.Auth;

public class UpdateThemePreferenceRequestValidatorTests
{
    private readonly UpdateThemePreferenceRequestValidator _validator = new();

    [Theory]
    [InlineData(ThemePreference.Light)]
    [InlineData(ThemePreference.Dark)]
    [InlineData(ThemePreference.System)]
    public void Valid_ThemePreference_Passes(ThemePreference preference)
    {
        var result = _validator.TestValidate(new UpdateThemePreferenceRequest(preference));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Out_Of_Range_ThemePreference_Fails()
    {
        var result = _validator.TestValidate(new UpdateThemePreferenceRequest((ThemePreference)99));

        result.ShouldHaveValidationErrorFor(x => x.ThemePreference);
    }
}
