using FluentValidation;

namespace Steward.Application.Tracking.ServiceRecords;

public class CreateServiceRecordRequestValidator : AbstractValidator<CreateServiceRecordRequest>
{
    public CreateServiceRecordRequestValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue);
        RuleFor(x => x.OdometerMiles).GreaterThanOrEqualTo(0).When(x => x.OdometerMiles.HasValue);
        RuleFor(x => x.EngineHours).GreaterThanOrEqualTo(0).When(x => x.EngineHours.HasValue);
    }
}

public class UpdateServiceRecordRequestValidator : AbstractValidator<UpdateServiceRecordRequest>
{
    public UpdateServiceRecordRequestValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Cost).GreaterThanOrEqualTo(0).When(x => x.Cost.HasValue);
        RuleFor(x => x.OdometerMiles).GreaterThanOrEqualTo(0).When(x => x.OdometerMiles.HasValue);
        RuleFor(x => x.EngineHours).GreaterThanOrEqualTo(0).When(x => x.EngineHours.HasValue);
    }
}
