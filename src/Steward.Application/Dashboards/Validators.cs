using FluentValidation;
using Steward.Domain.Enums;

namespace Steward.Application.Dashboards;

public class CreateDashboardRequestValidator : AbstractValidator<CreateDashboardRequest>
{
    public CreateDashboardRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class UpdateDashboardRequestValidator : AbstractValidator<UpdateDashboardRequest>
{
    public UpdateDashboardRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

public class ReplaceWidgetLayoutRequestValidator : AbstractValidator<ReplaceWidgetLayoutRequest>
{
    public ReplaceWidgetLayoutRequestValidator()
    {
        RuleForEach(x => x.Widgets).ChildRules(widget =>
        {
            widget.RuleFor(w => w.WidgetType).IsInEnum()
                .WithMessage("WidgetType is not a valid catalog value.");
            widget.RuleFor(w => w.WidgetSize).IsInEnum()
                .WithMessage("WidgetSize is not a valid value.");
            widget.RuleFor(w => w.Config)
                .Must(BeValidJsonOrNull)
                .When(w => w.Config is not null)
                .WithMessage("Config must be valid JSON when provided.");
        });
    }

    private static bool BeValidJsonOrNull(string? config)
    {
        if (config is null) return true;
        try
        {
            System.Text.Json.JsonDocument.Parse(config);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
