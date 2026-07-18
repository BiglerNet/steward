using Steward.Api.Common;
using Steward.Application.Assets;
using Steward.Application.Authorization;
using Steward.Application.Tracking.MaintenanceRecurrence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/assets/{assetId:guid}/maintenance-schedule")]
public class MaintenanceScheduleController(
    IMaintenanceScheduleService maintenanceScheduleService,
    IAssetService assetService,
    IAuthorizationService authorizationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(Guid householdId, Guid assetId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.View, cancellationToken);
        if (authResult is not null) return authResult;

        var schedule = await maintenanceScheduleService.GetScheduleAsync(assetId, cancellationToken);
        return Ok(schedule);
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
