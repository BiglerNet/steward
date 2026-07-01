using Steward.Application.Authorization;
using Steward.Domain.Enums;
using Steward.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Steward.Infrastructure.Authorization;

public class HouseholdAuthorizationHandler(StewardDbContext dbContext)
    : AuthorizationHandler<OperationAuthorizationRequirement, IHouseholdResource>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        IHouseholdResource resource)
    {
        if (context.User.IsInRole("PlatformAdmin"))
        {
            context.Succeed(requirement);
            return;
        }

        var userIdClaim = context.User.FindFirst("sub")?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var membership = await dbContext.HouseholdMemberships
            .AsNoTracking()
            .FirstOrDefaultAsync(m =>
                m.HouseholdId == resource.HouseholdId &&
                m.UserId == userId &&
                m.Status == HouseholdMemberStatus.Active);

        if (membership is null)
        {
            return;
        }

        var allowed = requirement.Name switch
        {
            nameof(HouseholdOperations.View) => true,
            nameof(HouseholdOperations.Edit) =>
                membership.Role is HouseholdMemberRole.Contributor or HouseholdMemberRole.Owner,
            nameof(HouseholdOperations.Delete) => membership.Role == HouseholdMemberRole.Owner,
            nameof(HouseholdOperations.Invite) => membership.Role == HouseholdMemberRole.Owner,
            _ => false,
        };

        if (allowed)
        {
            context.Succeed(requirement);
        }
    }
}
