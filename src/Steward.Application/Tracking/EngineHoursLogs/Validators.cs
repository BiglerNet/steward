using FluentValidation;

namespace Steward.Application.Tracking.EngineHoursLogs;

public class CreateEngineHoursLogRequestValidator : AbstractValidator<CreateEngineHoursLogRequest>
{
    public CreateEngineHoursLogRequestValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x).Must(x => x.HoursReading.HasValue || x.TripHours.HasValue)
            .WithMessage("At least one of hoursReading or tripHours is required.");
        RuleFor(x => x.HoursReading).GreaterThanOrEqualTo(0).When(x => x.HoursReading.HasValue);
        RuleFor(x => x.TripHours).GreaterThanOrEqualTo(0).When(x => x.TripHours.HasValue);
    }
}

public class UpdateEngineHoursLogRequestValidator : AbstractValidator<UpdateEngineHoursLogRequest>
{
    public UpdateEngineHoursLogRequestValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x).Must(x => x.HoursReading.HasValue || x.TripHours.HasValue)
            .WithMessage("At least one of hoursReading or tripHours is required.");
        RuleFor(x => x.HoursReading).GreaterThanOrEqualTo(0).When(x => x.HoursReading.HasValue);
        RuleFor(x => x.TripHours).GreaterThanOrEqualTo(0).When(x => x.TripHours.HasValue);
    }
}
