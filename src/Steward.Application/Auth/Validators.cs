using FluentValidation;

namespace Steward.Application.Auth;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");
        RuleFor(x => x.DisplayName).NotEmpty();
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class UpdateThemePreferenceRequestValidator : AbstractValidator<UpdateThemePreferenceRequest>
{
    public UpdateThemePreferenceRequestValidator()
    {
        RuleFor(x => x.ThemePreference).IsInEnum();
    }
}
