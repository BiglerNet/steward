using FluentValidation;

namespace Steward.Application.Tracking.Warranties;

public class CreateWarrantyRequestValidator : AbstractValidator<CreateWarrantyRequest>
{
    public CreateWarrantyRequestValidator()
    {
        RuleFor(x => x.Provider).NotEmpty();
    }
}

public class UpdateWarrantyRequestValidator : AbstractValidator<UpdateWarrantyRequest>
{
    public UpdateWarrantyRequestValidator()
    {
        RuleFor(x => x.Provider).NotEmpty();
    }
}
