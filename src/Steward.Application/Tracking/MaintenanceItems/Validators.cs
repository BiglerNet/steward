using FluentValidation;

namespace Steward.Application.Tracking.MaintenanceItems;

public class CreateMaintenanceItemRequestValidator : AbstractValidator<CreateMaintenanceItemRequest>
{
    public CreateMaintenanceItemRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue);
        RuleFor(x => x.OdometerMiles).GreaterThanOrEqualTo(0).When(x => x.OdometerMiles.HasValue);
        RuleFor(x => x.EngineHours).GreaterThanOrEqualTo(0).When(x => x.EngineHours.HasValue);
    }
}

public class PatchMaintenanceItemRequestValidator : AbstractValidator<PatchMaintenanceItemRequest>
{
    public PatchMaintenanceItemRequestValidator()
    {
        RuleFor(x => x.Title)
            .Must(t => !t.IsSet || !string.IsNullOrWhiteSpace(t.Value))
            .WithMessage("Title must not be empty.");
        RuleFor(x => x.Cost)
            .Must(c => !c.IsSet || c.Value is null || c.Value >= 0)
            .WithMessage("Cost must not be negative.");
        RuleFor(x => x.OdometerMiles)
            .Must(o => !o.IsSet || o.Value is null || o.Value >= 0)
            .WithMessage("OdometerMiles must not be negative.");
        RuleFor(x => x.EngineHours)
            .Must(h => !h.IsSet || h.Value is null || h.Value >= 0)
            .WithMessage("EngineHours must not be negative.");
    }
}

public class CreateChecklistItemRequestValidator : AbstractValidator<CreateChecklistItemRequest>
{
    public CreateChecklistItemRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty();
    }
}

public class PatchChecklistItemRequestValidator : AbstractValidator<PatchChecklistItemRequest>
{
    public PatchChecklistItemRequestValidator()
    {
        RuleFor(x => x.Text)
            .Must(t => !t.IsSet || !string.IsNullOrWhiteSpace(t.Value))
            .WithMessage("Text must not be empty.");
    }
}

public class CreatePartLineRequestValidator : AbstractValidator<CreatePartLineRequest>
{
    public CreatePartLineRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue);
    }
}

public class PatchPartLineRequestValidator : AbstractValidator<PatchPartLineRequest>
{
    public PatchPartLineRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(n => !n.IsSet || !string.IsNullOrWhiteSpace(n.Value))
            .WithMessage("Name must not be empty.");
        RuleFor(x => x.Quantity)
            .Must(q => !q.IsSet || q.Value > 0)
            .WithMessage("Quantity must be greater than zero.");
        RuleFor(x => x.Cost)
            .Must(c => !c.IsSet || c.Value is null || c.Value >= 0)
            .WithMessage("Cost must not be negative.");
    }
}
