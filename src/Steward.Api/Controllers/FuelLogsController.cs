using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Assets;
using Steward.Application.Authorization;
using Steward.Application.Tracking.FuelLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/assets/{assetId:guid}/fuel-logs")]
public class FuelLogsController(
    IFuelLogService fuelLogService,
    IAssetService assetService,
    IAuthorizationService authorizationService,
    IValidator<CreateFuelLogRequest> createValidator,
    IValidator<UpdateFuelLogRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid householdId, Guid assetId, CreateFuelLogRequest request, CancellationToken cancellationToken)
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

        var fuelLog = await fuelLogService.CreateAsync(assetId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, fuelLog);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        Guid householdId, Guid assetId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.View, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var fuelLogs = await fuelLogService.ListAsync(assetId, from, to, cancellationToken);
        return Ok(fuelLogs);
    }

    [HttpPut("{fuelLogId:guid}")]
    public async Task<IActionResult> Update(
        Guid householdId,
        Guid assetId,
        Guid fuelLogId,
        UpdateFuelLogRequest request,
        CancellationToken cancellationToken)
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

        var fuelLog = await fuelLogService.UpdateAsync(assetId, fuelLogId, request, cancellationToken);
        return Ok(fuelLog);
    }

    [HttpDelete("{fuelLogId:guid}")]
    public async Task<IActionResult> Delete(
        Guid householdId, Guid assetId, Guid fuelLogId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        await fuelLogService.DeleteAsync(assetId, fuelLogId, cancellationToken);
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
