using FluentValidation;

namespace Steward.Application.Households;

public class CreateHouseholdRequestValidator : AbstractValidator<CreateHouseholdRequest>
{
    public CreateHouseholdRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.PublicSlug)
            .NotEmpty()
            .Length(3, 60)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must be lowercase alphanumeric characters and hyphens only.");
    }
}

public class UpdateHouseholdRequestValidator : AbstractValidator<UpdateHouseholdRequest>
{
    public UpdateHouseholdRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.PublicSlug)
            .NotEmpty()
            .Length(3, 60)
            .Matches("^[a-z0-9-]+$").WithMessage("Slug must be lowercase alphanumeric characters and hyphens only.");
    }
}
