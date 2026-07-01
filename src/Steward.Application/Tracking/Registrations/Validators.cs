using FluentValidation;

namespace Steward.Application.Tracking.Registrations;

public class CreateRegistrationRequestValidator : AbstractValidator<CreateRegistrationRequest>
{
    public CreateRegistrationRequestValidator()
    {
        RuleFor(x => x.RegistrationNumber).NotEmpty();
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue);
    }
}

public class UpdateRegistrationRequestValidator : AbstractValidator<UpdateRegistrationRequest>
{
    public UpdateRegistrationRequestValidator()
    {
        RuleFor(x => x.RegistrationNumber).NotEmpty();
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue);
    }
}
