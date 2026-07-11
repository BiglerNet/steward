using FluentValidation;

namespace Steward.Application.Photos;

public class SetCoverPhotoRequestValidator : AbstractValidator<SetCoverPhotoRequest>
{
    public SetCoverPhotoRequestValidator()
    {
        RuleFor(x => x.PhotoId).NotEmpty();
    }
}
