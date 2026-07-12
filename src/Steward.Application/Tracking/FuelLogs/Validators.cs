using FluentValidation;
using Steward.Domain.Enums;

namespace Steward.Application.Tracking.FuelLogs;

public class CreateFuelLogRequestValidator : AbstractValidator<CreateFuelLogRequest>
{
    public CreateFuelLogRequestValidator()
    {
        RuleFor(x => x.LogType).IsInEnum();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Unit).IsInEnum();
        RuleFor(x => x.PricePerUnit).GreaterThanOrEqualTo(0).When(x => x.PricePerUnit.HasValue);
        RuleFor(x => x.TotalCost).GreaterThanOrEqualTo(0).When(x => x.TotalCost.HasValue);
        RuleFor(x => x.MilesAtLog).GreaterThanOrEqualTo(0).When(x => x.MilesAtLog.HasValue);
        RuleFor(x => x.HoursAtLog).GreaterThanOrEqualTo(0).When(x => x.HoursAtLog.HasValue);
    }
}

public class UpdateFuelLogRequestValidator : AbstractValidator<UpdateFuelLogRequest>
{
    public UpdateFuelLogRequestValidator()
    {
        RuleFor(x => x.LogType).IsInEnum();
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Unit).IsInEnum();
        RuleFor(x => x.PricePerUnit).GreaterThanOrEqualTo(0).When(x => x.PricePerUnit.HasValue);
        RuleFor(x => x.TotalCost).GreaterThanOrEqualTo(0).When(x => x.TotalCost.HasValue);
        RuleFor(x => x.MilesAtLog).GreaterThanOrEqualTo(0).When(x => x.MilesAtLog.HasValue);
        RuleFor(x => x.HoursAtLog).GreaterThanOrEqualTo(0).When(x => x.HoursAtLog.HasValue);
    }
}
