using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Authorization;
using Steward.Application.Households.Memberships;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}")]
public class HouseholdMembershipsController(
    Steward.Application.Households.IHouseholdService householdService,
    IAuthorizationService authorizationService,
    IValidator<InviteMemberRequest> inviteValidator) : ControllerBase
{
    [HttpGet("members")]
    public async Task<IActionResult> ListMembers(Guid householdId, CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(User, new HouseholdResource(householdId), HouseholdOperations.View);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var members = await householdService.ListMembersAsync(householdId, cancellationToken);
        return Ok(members);
    }

    [HttpPost("members/invite")]
    public async Task<IActionResult> Invite(Guid householdId, InviteMemberRequest request, CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(User, new HouseholdResource(householdId), HouseholdOperations.Invite);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var validation = await inviteValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        var invitation = await householdService.InviteMemberAsync(householdId, User.GetUserId(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, invitation);
    }

    [HttpDelete("invitations/{code}")]
    public async Task<IActionResult> RevokeInvitation(Guid householdId, string code, CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(User, new HouseholdResource(householdId), HouseholdOperations.Invite);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        await householdService.RevokeInviteAsync(householdId, code, cancellationToken);
        return NoContent();
    }

    [HttpDelete("members/{userId:guid}")]
    public async Task<IActionResult> RevokeMember(Guid householdId, Guid userId, CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(User, new HouseholdResource(householdId), HouseholdOperations.Invite);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        await householdService.RevokeMemberAsync(householdId, User.GetUserId(), userId, cancellationToken);
        return NoContent();
    }
}
