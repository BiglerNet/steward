using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Assets;
using Steward.Application.Assets.Engines;
using Steward.Application.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/assets/{assetId:guid}/engines")]
public class EnginesController(
    IEngineService engineService,
    IAssetService assetService,
    IAuthorizationService authorizationService,
    IValidator<CreateEngineRequest> createValidator,
    IValidator<UpdateEngineRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid householdId, Guid assetId, CreateEngineRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        var engine = await engineService.CreateAsync(assetId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, engine);
    }

    [HttpGet]
    public async Task<IActionResult> List(Guid householdId, Guid assetId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.View, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var engines = await engineService.ListAsync(assetId, cancellationToken);
        return Ok(engines);
    }

    [HttpPut("{engineId:guid}")]
    public async Task<IActionResult> Update(
        Guid householdId, Guid assetId, Guid engineId, UpdateEngineRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        var engine = await engineService.UpdateAsync(assetId, engineId, request, cancellationToken);
        return Ok(engine);
    }

    [HttpPost("{engineId:guid}/retire")]
    public async Task<IActionResult> Retire(
        Guid householdId, Guid assetId, Guid engineId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var engine = await engineService.RetireAsync(assetId, engineId, cancellationToken);
        return Ok(engine);
    }

    [HttpPost("{engineId:guid}/mark-broken")]
    public async Task<IActionResult> MarkBroken(
        Guid householdId, Guid assetId, Guid engineId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var engine = await engineService.MarkBrokenAsync(assetId, engineId, cancellationToken);
        return Ok(engine);
    }

    [HttpPost("{engineId:guid}/reactivate")]
    public async Task<IActionResult> Reactivate(
        Guid householdId, Guid assetId, Guid engineId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var engine = await engineService.ReactivateAsync(assetId, engineId, cancellationToken);
        return Ok(engine);
    }

    [HttpDelete("{engineId:guid}")]
    public async Task<IActionResult> Delete(
        Guid householdId, Guid assetId, Guid engineId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Delete, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        await engineService.DeleteAsync(assetId, engineId, cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult?> AuthorizeAsync(
        Guid householdId,
        Guid assetId,
        OperationAuthorizationRequirement operation,
        CancellationToken cancellationToken)
    {
        var actualHouseholdId = await assetService.GetHouseholdIdForAssetAsync(assetId, cancellationToken);
        if (actualHouseholdId != householdId)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, new HouseholdResource(householdId), operation);
        return authResult.Succeeded ? null : Forbid();
    }
}
