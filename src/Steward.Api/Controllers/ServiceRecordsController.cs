using FluentValidation;
using Steward.Api.Common;
using Steward.Application.Assets;
using Steward.Application.Authorization;
using Steward.Application.Tracking.ServiceRecords;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Steward.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/households/{householdId:guid}/assets/{assetId:guid}/service-records")]
public class ServiceRecordsController(
    IServiceRecordService serviceRecordService,
    IAssetService assetService,
    IAuthorizationService authorizationService,
    IValidator<CreateServiceRecordRequest> createValidator,
    IValidator<UpdateServiceRecordRequest> updateValidator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        Guid householdId, Guid assetId, CreateServiceRecordRequest request, CancellationToken cancellationToken)
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

        var serviceRecord = await serviceRecordService.CreateAsync(assetId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, serviceRecord);
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

        var serviceRecords = await serviceRecordService.ListAsync(assetId, from, to, cancellationToken);
        return Ok(serviceRecords);
    }

    [HttpPut("{serviceRecordId:guid}")]
    public async Task<IActionResult> Update(
        Guid householdId,
        Guid assetId,
        Guid serviceRecordId,
        UpdateServiceRecordRequest request,
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

        var serviceRecord = await serviceRecordService.UpdateAsync(assetId, serviceRecordId, request, cancellationToken);
        return Ok(serviceRecord);
    }

    [HttpDelete("{serviceRecordId:guid}")]
    public async Task<IActionResult> Delete(
        Guid householdId, Guid assetId, Guid serviceRecordId, CancellationToken cancellationToken)
    {
        var authResult = await AuthorizeAsync(householdId, assetId, HouseholdOperations.Edit, cancellationToken);
        if (authResult is not null)
        {
            return authResult;
        }

        await serviceRecordService.DeleteAsync(assetId, serviceRecordId, cancellationToken);
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
