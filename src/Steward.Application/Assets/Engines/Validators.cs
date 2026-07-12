using FluentValidation;
using Steward.Domain.Enums;

namespace Steward.Application.Assets.Engines;

public class CreateEngineRequestValidator : AbstractValidator<CreateEngineRequest>
{
    private static readonly int[] ValidOctaneValues = [87, 89, 91, 93];

    public CreateEngineRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty();
        RuleFor(x => x.Cylinders).GreaterThanOrEqualTo(0).When(x => x.Cylinders.HasValue);
        RuleFor(x => x.DisplacementCc).GreaterThanOrEqualTo(0).When(x => x.DisplacementCc.HasValue);
        RuleFor(x => x.InstalledAtAssetMiles).GreaterThanOrEqualTo(0).When(x => x.InstalledAtAssetMiles.HasValue);
        RuleFor(x => x.InstalledAtAssetHours).GreaterThanOrEqualTo(0).When(x => x.InstalledAtAssetHours.HasValue);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1).When(x => x.Year.HasValue);
        RuleFor(x => x.RecommendedOctane).Must(v => ValidOctaneValues.Contains(v!.Value))
            .When(x => x.RecommendedOctane.HasValue)
            .WithMessage("RecommendedOctane must be one of: 87, 89, 91, 93.");

        RuleFor(x => x.Mechanism)
            .Must(_ => false)
            .When(x => x.Mechanism.HasValue && x.EngineType != EngineType.Ice)
            .WithMessage("Mechanism is only valid when EngineType is Ice.");
        RuleFor(x => x.FuelType)
            .Must(_ => false)
            .When(x => x.FuelType.HasValue && x.EngineType != EngineType.Ice)
            .WithMessage("FuelType is only valid when EngineType is Ice.");
        RuleFor(x => x.IsExternallyChargeable)
            .Must(_ => false)
            .When(x => x.IsExternallyChargeable.HasValue && x.EngineType != EngineType.Electric)
            .WithMessage("IsExternallyChargeable is only valid when EngineType is Electric.");
        RuleFor(x => x.TwoStrokeOilDelivery)
            .Must(_ => false)
            .When(x => x.TwoStrokeOilDelivery.HasValue && x.Mechanism != Mechanism.TwoStroke)
            .WithMessage("TwoStrokeOilDelivery is only valid when Mechanism is TwoStroke.");
        RuleFor(x => x.TwoStrokeMixRatio)
            .Must(_ => false)
            .When(x => x.TwoStrokeMixRatio is not null && x.Mechanism != Mechanism.TwoStroke)
            .WithMessage("TwoStrokeMixRatio is only valid when Mechanism is TwoStroke.");
    }
}

public class UpdateEngineRequestValidator : AbstractValidator<UpdateEngineRequest>
{
    private static readonly int[] ValidOctaneValues = [87, 89, 91, 93];

    public UpdateEngineRequestValidator()
    {
        RuleFor(x => x.Label).NotEmpty();
        RuleFor(x => x.Cylinders).GreaterThanOrEqualTo(0).When(x => x.Cylinders.HasValue);
        RuleFor(x => x.DisplacementCc).GreaterThanOrEqualTo(0).When(x => x.DisplacementCc.HasValue);
        RuleFor(x => x.InstalledAtAssetMiles).GreaterThanOrEqualTo(0).When(x => x.InstalledAtAssetMiles.HasValue);
        RuleFor(x => x.InstalledAtAssetHours).GreaterThanOrEqualTo(0).When(x => x.InstalledAtAssetHours.HasValue);
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1).When(x => x.Year.HasValue);
        RuleFor(x => x.RecommendedOctane).Must(v => ValidOctaneValues.Contains(v!.Value))
            .When(x => x.RecommendedOctane.HasValue)
            .WithMessage("RecommendedOctane must be one of: 87, 89, 91, 93.");

        RuleFor(x => x.Mechanism)
            .Must(_ => false)
            .When(x => x.Mechanism.HasValue && x.EngineType != EngineType.Ice)
            .WithMessage("Mechanism is only valid when EngineType is Ice.");
        RuleFor(x => x.FuelType)
            .Must(_ => false)
            .When(x => x.FuelType.HasValue && x.EngineType != EngineType.Ice)
            .WithMessage("FuelType is only valid when EngineType is Ice.");
        RuleFor(x => x.IsExternallyChargeable)
            .Must(_ => false)
            .When(x => x.IsExternallyChargeable.HasValue && x.EngineType != EngineType.Electric)
            .WithMessage("IsExternallyChargeable is only valid when EngineType is Electric.");
        RuleFor(x => x.TwoStrokeOilDelivery)
            .Must(_ => false)
            .When(x => x.TwoStrokeOilDelivery.HasValue && x.Mechanism != Mechanism.TwoStroke)
            .WithMessage("TwoStrokeOilDelivery is only valid when Mechanism is TwoStroke.");
        RuleFor(x => x.TwoStrokeMixRatio)
            .Must(_ => false)
            .When(x => x.TwoStrokeMixRatio is not null && x.Mechanism != Mechanism.TwoStroke)
            .WithMessage("TwoStrokeMixRatio is only valid when Mechanism is TwoStroke.");
    }
}
