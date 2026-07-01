using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Authorization;
using Steward.Application.Households;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households")]
public class HouseholdsController(
    IHouseholdService householdService,
    IAuthorizationService authorizationService,
    IValidator<CreateHouseholdRequest> createValidator,
    IValidator<UpdateHouseholdRequest> updateValidator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var households = await householdService.ListForUserAsync(User.GetUserId(), cancellationToken);
        return Ok(households);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateHouseholdRequest request, CancellationToken cancellationToken)
    {
        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        var household = await householdService.CreateAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = household.Id }, household);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(User, new HouseholdResource(id), HouseholdOperations.View);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var household = await householdService.GetByIdAsync(User.GetUserId(), id, cancellationToken);
        return Ok(household);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateHouseholdRequest request, CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(User, new HouseholdResource(id), HouseholdOperations.Edit);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        var household = await householdService.UpdateAsync(id, request, cancellationToken);
        return Ok(household);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(User, new HouseholdResource(id), HouseholdOperations.Delete);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        await householdService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
