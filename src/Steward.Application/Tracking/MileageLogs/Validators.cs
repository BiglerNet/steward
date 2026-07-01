using FluentValidation;

namespace Steward.Application.Tracking.MileageLogs;

public class CreateMileageLogRequestValidator : AbstractValidator<CreateMileageLogRequest>
{
    public CreateMileageLogRequestValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x).Must(x => x.OdometerReading.HasValue || x.TripMiles.HasValue)
            .WithMessage("At least one of odometerReading or tripMiles is required.");
        RuleFor(x => x.OdometerReading).GreaterThanOrEqualTo(0).When(x => x.OdometerReading.HasValue);
        RuleFor(x => x.TripMiles).GreaterThanOrEqualTo(0).When(x => x.TripMiles.HasValue);
    }
}

public class UpdateMileageLogRequestValidator : AbstractValidator<UpdateMileageLogRequest>
{
    public UpdateMileageLogRequestValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x).Must(x => x.OdometerReading.HasValue || x.TripMiles.HasValue)
            .WithMessage("At least one of odometerReading or tripMiles is required.");
        RuleFor(x => x.OdometerReading).GreaterThanOrEqualTo(0).When(x => x.OdometerReading.HasValue);
        RuleFor(x => x.TripMiles).GreaterThanOrEqualTo(0).When(x => x.TripMiles.HasValue);
    }
}
