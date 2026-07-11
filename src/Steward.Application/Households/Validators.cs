using FluentValidation;
using Steward.Application.Regions;

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

        HouseholdLocationRules.AddRules(this);
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

        HouseholdLocationRules.AddRules(this);
    }
}

internal static class HouseholdLocationRules
{
    public static void AddRules<T>(AbstractValidator<T> validator) where T : IHouseholdLocation
    {
        validator.RuleFor(x => x.Country!)
            .Must(RegionRegistry.IsValidCountry)
            .When(x => x.Country is not null)
            .WithMessage("Unknown country code.");

        validator.RuleFor(x => x).Custom((request, context) =>
        {
            if (request.Region is null)
            {
                return;
            }

            if (request.Country is null || !RegionRegistry.IsValidRegion(request.Country, request.Region))
            {
                context.AddFailure(nameof(IHouseholdLocation.Region),
                    "Region does not belong to the specified country, or country is missing.");
            }
        });
    }
}

public interface IHouseholdLocation
{
    string? Country { get; }
    string? Region { get; }
}
