using FluentValidation;
using Steward.Domain.Enums;

namespace Steward.Application.Households.Memberships;

public class InviteMemberRequestValidator : AbstractValidator<InviteMemberRequest>
{
    public InviteMemberRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Role)
            .IsInEnum()
            .NotEqual(HouseholdMemberRole.Owner).WithMessage("Owner role cannot be assigned via invite.");
    }
}
