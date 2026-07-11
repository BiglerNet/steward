using Steward.Application.Households;
using Steward.Application.PlatformAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize(Roles = "PlatformAdmin")]
[ApiController]
[Route("api/admin/households")]
public class PlatformAdminHouseholdsController(IHouseholdService householdService) : ControllerBase
{
    [HttpPut("{householdId:guid}/storage-quota")]
    public async Task<IActionResult> SetStorageQuota(
        Guid householdId, SetStorageQuotaRequest request, CancellationToken cancellationToken)
    {
        await householdService.SetStorageQuotaOverrideAsync(householdId, request.QuotaBytes, cancellationToken);
        return Ok();
    }
}
