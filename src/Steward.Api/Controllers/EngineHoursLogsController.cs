using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Assets.Engines;
using Steward.Application.Authorization;
using Steward.Application.Tracking.EngineHoursLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/assets/{assetId:guid}/engines/{engineId:guid}/hours-logs")]
public class EngineHoursLogsController(
    IEngineHoursLogService engineHoursLogService,
    IEngineService engineService,
    IAuthorizationService authorizationService,
    IValidator<CreateEngineHoursLogRequest> createValidator,
    IValidator<UpdateEngineHoursLogRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid householdId, Guid assetId, Guid engineId, CreateEngineHoursLogRequest request, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, engineId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var validation = await createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        var hoursLog = await engineHoursLogService.CreateAsync(engineId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, hoursLog);
    }

    [HttpGet]
    public async Task<IActionResult> List(
        Guid householdId,
        Guid assetId,
        Guid engineId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, engineId, HouseholdOperations.View, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var hoursLogs = await engineHoursLogService.ListAsync(engineId, from, to, cancellationToken);
        return Ok(hoursLogs);
    }

    [HttpPut("{hoursLogId:guid}")]
    public async Task<IActionResult> Update(
        Guid householdId,
        Guid assetId,
        Guid engineId,
        Guid hoursLogId,
        UpdateEngineHoursLogRequest request,
        CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, engineId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        var validation = await updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ValidationProblem(validation.ToModelState());
        }

        var hoursLog = await engineHoursLogService.UpdateAsync(engineId, hoursLogId, request, cancellationToken);
        return Ok(hoursLog);
    }

    [HttpDelete("{hoursLogId:guid}")]
    public async Task<IActionResult> Delete(
        Guid householdId, Guid assetId, Guid engineId, Guid hoursLogId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, engineId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        await engineHoursLogService.DeleteAsync(engineId, hoursLogId, cancellationToken);
        return NoContent();
    }

    private async Task<IActionResult?> AuthorizeAsync(
        Guid householdId,
        Guid assetId,
        Guid engineId,
        OperationAuthorizationRequirement operation,
        CancellationToken cancellationToken)
    {
        var actualHouseholdId = await engineService.GetHouseholdIdForEngineAsync(assetId, engineId, cancellationToken);
        if (actualHouseholdId != householdId)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, new HouseholdResource(householdId), operation);
        return authResult.Succeeded ? null : Forbid();
    }
}
