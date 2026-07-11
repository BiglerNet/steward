using FluentValidation;
using FluentValidation.Results;
using Steward.Application.AssetTypes;

namespace Steward.Application.Assets;

public class CreateAssetRequestValidator : AbstractValidator<CreateAssetRequest>
{
    public CreateAssetRequestValidator()
    {
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1).When(x => x.Year.HasValue);
        RuleFor(x => x.UsageTrackingMode!.Value).IsInEnum().When(x => x.UsageTrackingMode.HasValue);

        AssetValidatorRules.AddPositiveNumberRules(this);
        AssetValidatorRules.AddTypeSpecificEnumRules(this);

        RuleFor(x => x).Custom((request, context) =>
        {
            if (!Enum.IsDefined(request.Category))
            {
                return;
            }

            foreach (var field in AssetTypeFieldCheck.FindInapplicableFields(request.Category, request))
            {
                context.AddFailure(new ValidationFailure(
                    field, AssetTypeFieldCheck.InapplicableMessage(field, request.Category)));
            }
        });
    }
}

public class UpdateAssetRequestValidator : AbstractValidator<UpdateAssetRequest>
{
    public UpdateAssetRequestValidator()
    {
        RuleFor(x => x.Category!.Value).IsInEnum().When(x => x.Category.HasValue);
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1).When(x => x.Year.HasValue);
        RuleFor(x => x.UsageTrackingMode).IsInEnum();

        AssetValidatorRules.AddPositiveNumberRules(this);
        AssetValidatorRules.AddTypeSpecificEnumRules(this);

        // Field applicability on update is checked in the service against the stored
        // asset's category, which the request may omit.
    }
}

internal static class AssetValidatorRules
{
    /// Numeric type-specific fields must be positive whenever present; whether they are
    /// allowed at all for the category is covered by the applicability check.
    public static void AddPositiveNumberRules<T>(AbstractValidator<T> validator) where T : IAssetTypeFields
    {
        validator.RuleFor(x => x.TrackLengthIn).GreaterThan(0).When(x => x.TrackLengthIn.HasValue);
        validator.RuleFor(x => x.LengthFt).GreaterThan(0).When(x => x.LengthFt.HasValue);
        validator.RuleFor(x => x.BeamFt).GreaterThan(0).When(x => x.BeamFt.HasValue);
        validator.RuleFor(x => x.BallSizeIn).GreaterThan(0).When(x => x.BallSizeIn.HasValue);
        validator.RuleFor(x => x.MaxLoadLbs).GreaterThan(0).When(x => x.MaxLoadLbs.HasValue);
        validator.RuleFor(x => x.InteriorHeightFt).GreaterThan(0).When(x => x.InteriorHeightFt.HasValue);
        validator.RuleFor(x => x.InteriorLengthFt).GreaterThan(0).When(x => x.InteriorLengthFt.HasValue);
        validator.RuleFor(x => x.CuttingWidthIn).GreaterThan(0).When(x => x.CuttingWidthIn.HasValue);
        validator.RuleFor(x => x.MaxPsi).GreaterThan(0).When(x => x.MaxPsi.HasValue);
        validator.RuleFor(x => x.MaxGpm).GreaterThan(0).When(x => x.MaxGpm.HasValue);
        validator.RuleFor(x => x.MastHeightFt).GreaterThan(0).When(x => x.MastHeightFt.HasValue);
        validator.RuleFor(x => x.MastCount).GreaterThan(0).When(x => x.MastCount.HasValue);
    }

    /// Enum-backed type-specific fields must be valid enum members whenever present;
    /// whether they are allowed at all for the category is covered by the applicability check.
    public static void AddTypeSpecificEnumRules<T>(AbstractValidator<T> validator) where T : IAssetTypeFields
    {
        validator.RuleFor(x => x.HullType!.Value).IsInEnum().When(x => x.HullType.HasValue);
        validator.RuleFor(x => x.DriveType!.Value).IsInEnum().When(x => x.DriveType.HasValue);
    }
}
