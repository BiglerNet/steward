using FluentValidation;

namespace Steward.Application.Tracking.Registrations;

public class CreateRegistrationRequestValidator : AbstractValidator<CreateRegistrationRequest>
{
    public CreateRegistrationRequestValidator()
    {
        RuleFor(x => x.Kind).NotNull();
        RuleFor(x => x.Kind!.Value).IsInEnum().When(x => x.Kind.HasValue);
        RuleFor(x => x.RegistrationNumber).MaximumLength(100);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue);
    }
}

public class UpdateRegistrationRequestValidator : AbstractValidator<UpdateRegistrationRequest>
{
    public UpdateRegistrationRequestValidator()
    {
        RuleFor(x => x.Kind).NotNull();
        RuleFor(x => x.Kind!.Value).IsInEnum().When(x => x.Kind.HasValue);
        RuleFor(x => x.RegistrationNumber).MaximumLength(100);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue);
    }
}
