using FluentValidation;
using Steward.Domain.Enums;

namespace Steward.Application.Assets;

public class CreateAssetRequestValidator : AbstractValidator<CreateAssetRequest>
{
    public CreateAssetRequestValidator()
    {
        RuleFor(x => x.AssetType).IsInEnum();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1).When(x => x.Year.HasValue);

        RuleFor(x => x.LengthFt).GreaterThan(0).When(x => x.AssetType == AssetType.Boat && x.LengthFt.HasValue);
        RuleFor(x => x.BeamFt).GreaterThan(0).When(x => x.AssetType == AssetType.Boat && x.BeamFt.HasValue);

        RuleFor(x => x.TrackLengthIn).GreaterThan(0)
            .When(x => x.AssetType == AssetType.Snowmobile && x.TrackLengthIn.HasValue);

        RuleFor(x => x.BallSizeIn).GreaterThan(0)
            .When(x => x.AssetType == AssetType.SnowmobileTrailer && x.BallSizeIn.HasValue);
        RuleFor(x => x.MaxLoadLbs).GreaterThan(0)
            .When(x => x.AssetType == AssetType.SnowmobileTrailer && x.MaxLoadLbs.HasValue);

        RuleFor(x => x.InteriorHeightFt).GreaterThan(0)
            .When(x => x.AssetType == AssetType.EnclosedTrailer && x.InteriorHeightFt.HasValue);
        RuleFor(x => x.InteriorLengthFt).GreaterThan(0)
            .When(x => x.AssetType == AssetType.EnclosedTrailer && x.InteriorLengthFt.HasValue);

        RuleFor(x => x.CuttingWidthIn).GreaterThan(0)
            .When(x => x.AssetType == AssetType.RidingMower && x.CuttingWidthIn.HasValue);

        RuleFor(x => x.MaxPsi).GreaterThan(0)
            .When(x => x.AssetType == AssetType.PowerWasher && x.MaxPsi.HasValue);
        RuleFor(x => x.MaxGpm).GreaterThan(0)
            .When(x => x.AssetType == AssetType.PowerWasher && x.MaxGpm.HasValue);
    }
}

public class UpdateAssetRequestValidator : AbstractValidator<UpdateAssetRequest>
{
    public UpdateAssetRequestValidator()
    {
        RuleFor(x => x.AssetType!.Value).IsInEnum().When(x => x.AssetType.HasValue);
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Year).InclusiveBetween(1900, DateTime.UtcNow.Year + 1).When(x => x.Year.HasValue);

        RuleFor(x => x.LengthFt).GreaterThan(0)
            .When(x => x.AssetType == AssetType.Boat && x.LengthFt.HasValue);
        RuleFor(x => x.BeamFt).GreaterThan(0)
            .When(x => x.AssetType == AssetType.Boat && x.BeamFt.HasValue);

        RuleFor(x => x.TrackLengthIn).GreaterThan(0)
            .When(x => x.AssetType == AssetType.Snowmobile && x.TrackLengthIn.HasValue);

        RuleFor(x => x.BallSizeIn).GreaterThan(0)
            .When(x => x.AssetType == AssetType.SnowmobileTrailer && x.BallSizeIn.HasValue);
        RuleFor(x => x.MaxLoadLbs).GreaterThan(0)
            .When(x => x.AssetType == AssetType.SnowmobileTrailer && x.MaxLoadLbs.HasValue);

        RuleFor(x => x.InteriorHeightFt).GreaterThan(0)
            .When(x => x.AssetType == AssetType.EnclosedTrailer && x.InteriorHeightFt.HasValue);
        RuleFor(x => x.InteriorLengthFt).GreaterThan(0)
            .When(x => x.AssetType == AssetType.EnclosedTrailer && x.InteriorLengthFt.HasValue);

        RuleFor(x => x.CuttingWidthIn).GreaterThan(0)
            .When(x => x.AssetType == AssetType.RidingMower && x.CuttingWidthIn.HasValue);

        RuleFor(x => x.MaxPsi).GreaterThan(0)
            .When(x => x.AssetType == AssetType.PowerWasher && x.MaxPsi.HasValue);
        RuleFor(x => x.MaxGpm).GreaterThan(0)
            .When(x => x.AssetType == AssetType.PowerWasher && x.MaxGpm.HasValue);
    }
}
