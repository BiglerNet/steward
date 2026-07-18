using Steward.Application.Authorization;
using Steward.Application.Tracking.MaintenanceItems;
using Steward.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/maintenance-items")]
public class HouseholdMaintenanceItemsController(
    IMaintenanceItemService maintenanceItemService,
    IAuthorizationService authorizationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        Guid householdId,
        [FromQuery(Name = "status")] MaintenanceItemStatus[]? status,
        [FromQuery] Guid? assetId,
        CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.AuthorizeAsync(
            User, new HouseholdResource(householdId), HouseholdOperations.View);
        if (!authResult.Succeeded) return Forbid();

        var items = await maintenanceItemService.ListForHouseholdAsync(householdId, status, assetId, cancellationToken);
        return Ok(items);
    }
}
