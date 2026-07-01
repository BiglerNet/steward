using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Assets;
using Steward.Application.Authorization;
using Steward.Application.Tracking.MileageLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/assets/{assetId:guid}/mileage-logs")]
public class MileageLogsController(
    IMileageLogService mileageLogService,
    IAssetService assetService,
    IAuthorizationService authorizationService,
    IValidator<CreateMileageLogRequest> createValidator,
    IValidator<UpdateMileageLogRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid householdId, Guid assetId, CreateMileageLogRequest request, CancellationToken cancellationToken)
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

        var mileageLog = await mileageLogService.CreateAsync(assetId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, mileageLog);
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

        var mileageLogs = await mileageLogService.ListAsync(assetId, from, to, cancellationToken);
        return Ok(mileageLogs);
    }

    [HttpPut("{mileageLogId:guid}")]
    public async Task<IActionResult> Update(
        Guid householdId,
        Guid assetId,
        Guid mileageLogId,
        UpdateMileageLogRequest request,
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

        var mileageLog = await mileageLogService.UpdateAsync(assetId, mileageLogId, request, cancellationToken);
        return Ok(mileageLog);
    }

    [HttpDelete("{mileageLogId:guid}")]
    public async Task<IActionResult> Delete(
        Guid householdId, Guid assetId, Guid mileageLogId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        await mileageLogService.DeleteAsync(assetId, mileageLogId, cancellationToken);
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
