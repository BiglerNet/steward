using FluentValidation;

namespace Steward.Application.Tracking.Templates;

public class CreateTemplateRequestValidator : AbstractValidator<CreateTemplateRequest>
{
    public CreateTemplateRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty();
    }
}

public class PatchTemplateRequestValidator : AbstractValidator<PatchTemplateRequest>
{
    public PatchTemplateRequestValidator()
    {
        RuleFor(x => x.Title)
            .Must(t => !t.IsSet || !string.IsNullOrWhiteSpace(t.Value))
            .WithMessage("Title must not be empty.");
    }
}

public class CreateTemplateStepRequestValidator : AbstractValidator<CreateTemplateStepRequest>
{
    public CreateTemplateStepRequestValidator()
    {
        RuleFor(x => x.Text).NotEmpty();
    }
}

public class PatchTemplateStepRequestValidator : AbstractValidator<PatchTemplateStepRequest>
{
    public PatchTemplateStepRequestValidator()
    {
        RuleFor(x => x.Text)
            .Must(t => !t.IsSet || !string.IsNullOrWhiteSpace(t.Value))
            .WithMessage("Text must not be empty.");
    }
}

public class DuplicateTemplateRequestValidator : AbstractValidator<DuplicateTemplateRequest>
{
    public DuplicateTemplateRequestValidator()
    {
        RuleFor(x => x.PlatformTemplateId).NotEmpty();
    }
}
